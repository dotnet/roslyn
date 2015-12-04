' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.MethodXml
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Partial Friend Class VisualBasicCodeModelService
        Inherits AbstractCodeModelService

        Private ReadOnly _commitBufferManagerFactory As CommitBufferManagerFactory

        Friend Sub New(provider As HostLanguageServices, editorOptionsFactoryService As IEditorOptionsFactoryService, refactorNotifyServices As IEnumerable(Of IRefactorNotifyService), commitBufferManagerFactory As CommitBufferManagerFactory)
            MyBase.New(
                provider,
                editorOptionsFactoryService,
                refactorNotifyServices,
                New LineAdjustmentFormattingRule(),
                New EndRegionFormattingRule())

            Me._commitBufferManagerFactory = commitBufferManagerFactory
        End Sub

        Private Shared ReadOnly s_codeTypeRefAsFullNameFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.ExpandNullable)

        Private Shared ReadOnly s_codeTypeRefAsStringFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Private Shared ReadOnly s_externalNameFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.ExpandNullable,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeName)

        Private Shared ReadOnly s_externalfullNameFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.ExpandNullable,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeName)

        Private Shared ReadOnly s_setTypeFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.ExpandNullable Or SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Private Shared ReadOnly s_raiseEventSignatureFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeParamsRefOut Or SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Private Shared Function IsNameableNode(node As SyntaxNode) As Boolean
            Select Case node.Kind
                Case SyntaxKind.Attribute,
                     SyntaxKind.ClassBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement,
                     SyntaxKind.EnumBlock,
                     SyntaxKind.EnumMemberDeclaration,
                     SyntaxKind.EventBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.ModuleBlock,
                     SyntaxKind.NamespaceBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.Parameter,
                     SyntaxKind.PropertyBlock,
                     SyntaxKind.StructureBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.OptionStatement,
                     SyntaxKind.SimpleImportsClause,
                     SyntaxKind.InheritsStatement,
                     SyntaxKind.ImplementsStatement

                    Return True

                Case SyntaxKind.NameColonEquals
                    Return True

                Case SyntaxKind.SimpleArgument,
                     SyntaxKind.OmittedArgument
                    ' Only arguments in attributes are valid
                    Return node.FirstAncestorOrSelf(Of AttributeSyntax) IsNot Nothing

                Case SyntaxKind.ModifiedIdentifier
                    Return node.FirstAncestorOrSelf(Of FieldDeclarationSyntax)() IsNot Nothing

                Case SyntaxKind.EventStatement
                    ' Only top-level event statements that aren't included in an event block are valid (e.g. single line events)
                    Return node.FirstAncestorOrSelf(Of EventBlockSyntax)() Is Nothing

                Case SyntaxKind.PropertyStatement
                    ' Only top-level property statements that aren't included in an property block are valid (e.g. auto-properties)
                    Return node.FirstAncestorOrSelf(Of PropertyBlockSyntax)() Is Nothing

                Case SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement
                    Return node.FirstAncestorOrSelf(Of MethodBlockSyntax)() Is Nothing


                Case Else
                    Return False
            End Select
        End Function

        Public Overrides Function GetElementKind(node As SyntaxNode) As EnvDTE.vsCMElement
            Select Case node.Kind
                Case SyntaxKind.ModuleBlock
                    Return EnvDTE.vsCMElement.vsCMElementModule
                Case SyntaxKind.ClassBlock
                    Return EnvDTE.vsCMElement.vsCMElementClass
                Case Else
                    Debug.Fail("Unsupported element kind" & CType(node.Kind, SyntaxKind))
                    Throw Exceptions.ThrowEInvalidArg()
            End Select
        End Function

        Public Overrides Function MatchesScope(node As SyntaxNode, scope As EnvDTE.vsCMElement) As Boolean
            'TODO: This has been copied from CSharpCodeModelService. Tweak to implement VB semantics.
            Select Case node.Kind
                Case SyntaxKind.NamespaceBlock
                    If scope = EnvDTE.vsCMElement.vsCMElementNamespace AndAlso
                        node.Parent IsNot Nothing Then
                        Return True
                    End If

                Case SyntaxKind.ModuleBlock
                    If scope = EnvDTE.vsCMElement.vsCMElementModule Then
                        Return True
                    End If

                Case SyntaxKind.ClassBlock
                    If scope = EnvDTE.vsCMElement.vsCMElementClass Then
                        Return True
                    End If
                Case SyntaxKind.StructureBlock
                    If scope = EnvDTE.vsCMElement.vsCMElementStruct Then
                        Return True
                    End If

                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    If scope = EnvDTE.vsCMElement.vsCMElementFunction AndAlso
                        node.FirstAncestorOrSelf(Of MethodBlockSyntax)() Is Nothing Then
                        Return True
                    End If

                Case SyntaxKind.ConstructorBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    If scope = EnvDTE.vsCMElement.vsCMElementFunction Then
                        Return True
                    End If

                Case SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DeclareSubStatement
                    If scope = EnvDTE.vsCMElement.vsCMElementDeclareDecl Then
                        Return True
                    End If

                Case SyntaxKind.EnumMemberDeclaration
                    If scope = EnvDTE.vsCMElement.vsCMElementVariable Then
                        Return True
                    End If

                Case SyntaxKind.FieldDeclaration
                    If scope = EnvDTE.vsCMElement.vsCMElementVariable Then
                        Return True
                    End If

                Case SyntaxKind.EventBlock
                    If scope = EnvDTE.vsCMElement.vsCMElementEvent Then
                        Return True
                    End If

                Case SyntaxKind.EventStatement
                    If Not TypeOf node.Parent Is EventBlockSyntax Then
                        If scope = EnvDTE.vsCMElement.vsCMElementEvent Then
                            Return True
                        End If
                    End If

                Case SyntaxKind.PropertyBlock
                    If scope = EnvDTE.vsCMElement.vsCMElementProperty Then
                        Return True
                    End If

                Case SyntaxKind.PropertyStatement
                    If Not TypeOf node.Parent Is PropertyBlockSyntax Then
                        If scope = EnvDTE.vsCMElement.vsCMElementProperty Then
                            Return True
                        End If
                    End If

                Case SyntaxKind.Attribute
                    If scope = EnvDTE.vsCMElement.vsCMElementAttribute Then
                        Return True
                    End If

                Case SyntaxKind.InterfaceBlock
                    If scope = EnvDTE.vsCMElement.vsCMElementInterface Then
                        Return True
                    End If

                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    If scope = EnvDTE.vsCMElement.vsCMElementDelegate Then
                        Return True
                    End If

                Case SyntaxKind.EnumBlock
                    If scope = EnvDTE.vsCMElement.vsCMElementEnum Then
                        Return True
                    End If

                Case SyntaxKind.StructureBlock
                    If scope = EnvDTE.vsCMElement.vsCMElementStruct Then
                        Return True
                    End If

                Case SyntaxKind.SimpleImportsClause
                    If scope = EnvDTE.vsCMElement.vsCMElementImportStmt Then
                        Return True
                    End If

                Case SyntaxKind.ModifiedIdentifier,
                     SyntaxKind.VariableDeclarator
                    If node.Parent.Kind <> SyntaxKind.Parameter Then
                        ' The parent of an identifier/variable declarator may be a
                        ' field. If the parent matches the desired scope, then this
                        ' node matches as well.
                        Return MatchesScope(node.Parent, scope)
                    End If

                Case SyntaxKind.Parameter
                    If scope = EnvDTE.vsCMElement.vsCMElementParameter Then
                        Return True
                    End If

                Case SyntaxKind.OptionStatement
                    If scope = EnvDTE.vsCMElement.vsCMElementOptionStmt Then
                        Return True
                    End If

                Case SyntaxKind.InheritsStatement
                    If scope = EnvDTE.vsCMElement.vsCMElementInheritsStmt Then
                        Return True
                    End If

                Case SyntaxKind.ImplementsStatement
                    If scope = EnvDTE.vsCMElement.vsCMElementImplementsStmt Then
                        Return True
                    End If

                Case Else
                    Return False
            End Select

            Return False
        End Function

        Public Overrides Function GetOptionNodes(parent As SyntaxNode) As IEnumerable(Of SyntaxNode)
            If TypeOf parent Is CompilationUnitSyntax Then
                Return DirectCast(parent, CompilationUnitSyntax).Options.AsEnumerable()
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
        End Function

        Private Overloads Shared Function GetImportNodes(parent As CompilationUnitSyntax) As IEnumerable(Of SyntaxNode)
            Dim result = New List(Of SyntaxNode)

            For Each importStatement In parent.Imports
                For Each importClause In importStatement.ImportsClauses
                    ' NOTE: XmlNamespaceImportsClause is not support by VB Code Model
                    If importClause.IsKind(SyntaxKind.SimpleImportsClause) Then
                        result.Add(importClause)
                    End If
                Next
            Next

            Return result
        End Function

        Public Overrides Function GetImportNodes(parent As SyntaxNode) As IEnumerable(Of SyntaxNode)
            If TypeOf parent Is CompilationUnitSyntax Then
                Return GetImportNodes(DirectCast(parent, CompilationUnitSyntax))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
        End Function

        Private Overloads Shared Function GetAttributeNodes(attributesBlockList As SyntaxList(Of AttributeListSyntax)) As IEnumerable(Of SyntaxNode)
            Dim result = New List(Of SyntaxNode)

            For Each attributeBlock In attributesBlockList
                For Each attribute In attributeBlock.Attributes
                    result.Add(attribute)
                Next
            Next

            Return result
        End Function

        Private Overloads Shared Function GetAttributeNodes(attributesStatementList As SyntaxList(Of AttributesStatementSyntax)) As IEnumerable(Of SyntaxNode)
            Dim result = New List(Of SyntaxNode)

            For Each attributesStatement In attributesStatementList
                For Each attributeBlock In attributesStatement.AttributeLists
                    For Each attribute In attributeBlock.Attributes
                        result.Add(attribute)
                    Next
                Next
            Next

            Return result
        End Function

        Public Overrides Function GetAttributeNodes(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            If TypeOf node Is CompilationUnitSyntax Then
                Return GetAttributeNodes(DirectCast(node, CompilationUnitSyntax).Attributes)
            ElseIf TypeOf node Is TypeBlockSyntax Then
                Return GetAttributeNodes(DirectCast(node, TypeBlockSyntax).BlockStatement.AttributeLists)
            ElseIf TypeOf node Is EnumBlockSyntax Then
                Return GetAttributeNodes(DirectCast(node, EnumBlockSyntax).EnumStatement.AttributeLists)
            ElseIf TypeOf node Is DelegateStatementSyntax Then
                Return GetAttributeNodes(DirectCast(node, DelegateStatementSyntax).AttributeLists)
            ElseIf TypeOf node Is DeclareStatementSyntax Then
                Return GetAttributeNodes(DirectCast(node, DeclareStatementSyntax).AttributeLists)
            ElseIf TypeOf node Is MethodStatementSyntax Then
                Return GetAttributeNodes(DirectCast(node, MethodStatementSyntax).AttributeLists)
            ElseIf TypeOf node Is MethodBlockBaseSyntax Then
                Return GetAttributeNodes(DirectCast(node, MethodBlockBaseSyntax).BlockStatement.AttributeLists)
            ElseIf TypeOf node Is PropertyBlockSyntax Then
                Return GetAttributeNodes(DirectCast(node, PropertyBlockSyntax).PropertyStatement.AttributeLists)
            ElseIf TypeOf node Is PropertyStatementSyntax Then
                Return GetAttributeNodes(DirectCast(node, PropertyStatementSyntax).AttributeLists)
            ElseIf TypeOf node Is EventBlockSyntax Then
                Return GetAttributeNodes(DirectCast(node, EventBlockSyntax).EventStatement.AttributeLists)
            ElseIf TypeOf node Is EventStatementSyntax Then
                Return GetAttributeNodes(DirectCast(node, EventStatementSyntax).AttributeLists)
            ElseIf TypeOf node Is FieldDeclarationSyntax Then
                Return GetAttributeNodes(DirectCast(node, FieldDeclarationSyntax).AttributeLists)
            ElseIf TypeOf node Is ParameterSyntax Then
                Return GetAttributeNodes(DirectCast(node, ParameterSyntax).AttributeLists)
            ElseIf TypeOf node Is ModifiedIdentifierSyntax OrElse
                   TypeOf node Is VariableDeclaratorSyntax Then
                Return GetAttributeNodes(node.Parent)
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
        End Function

        Public Overrides Function GetAttributeArgumentNodes(parent As SyntaxNode) As IEnumerable(Of SyntaxNode)
            If TypeOf parent Is AttributeSyntax Then
                Dim attribute = DirectCast(parent, AttributeSyntax)

                If attribute.ArgumentList Is Nothing Then
                    Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
                End If

                Return attribute.ArgumentList.Arguments
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
        End Function

        Public Overrides Function GetInheritsNodes(parent As SyntaxNode) As IEnumerable(Of SyntaxNode)
            If TypeOf parent Is TypeBlockSyntax Then
                Dim typeBlock = DirectCast(parent, TypeBlockSyntax)

                Return typeBlock.Inherits.AsEnumerable()
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
        End Function

        Public Overrides Function GetImplementsNodes(parent As SyntaxNode) As IEnumerable(Of SyntaxNode)
            If TypeOf parent Is TypeBlockSyntax Then
                Dim typeBlock = DirectCast(parent, TypeBlockSyntax)

                Return typeBlock.Implements.AsEnumerable()
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
        End Function

        Private Shared Function IsContainerNode(container As SyntaxNode) As Boolean
            Return TypeOf container Is CompilationUnitSyntax OrElse
                   TypeOf container Is NamespaceBlockSyntax OrElse
                   TypeOf container Is TypeBlockSyntax OrElse
                   TypeOf container Is EnumBlockSyntax
        End Function

        Private Shared Iterator Function GetChildMemberNodes(container As SyntaxNode) As IEnumerable(Of SyntaxNode)
            If TypeOf container Is CompilationUnitSyntax Then
                For Each member In DirectCast(container, CompilationUnitSyntax).Members
                    Yield member
                Next
            ElseIf TypeOf container Is NamespaceBlockSyntax Then

                For Each member In DirectCast(container, NamespaceBlockSyntax).Members
                    Yield member
                Next
            ElseIf TypeOf container Is TypeBlockSyntax Then

                For Each member In DirectCast(container, TypeBlockSyntax).Members
                    Yield member
                Next
            ElseIf TypeOf container Is EnumBlockSyntax Then

                For Each member In DirectCast(container, EnumBlockSyntax).Members
                    Yield member
                Next
            End If
        End Function

        Private Shared Function NodeIsSupported(test As Boolean, node As SyntaxNode) As Boolean
            Return Not test OrElse IsNameableNode(node)
        End Function


        ''' <summary>
        ''' Retrieves the members of a specified <paramref name="container"/> node. The members that are
        ''' returned can be controlled by passing various parameters.
        ''' </summary>
        ''' <param name="container">The <see cref="SyntaxNode"/> from which to retrieve members.</param>
        ''' <param name="includeSelf">If true, the container Is returned as well.</param>
        ''' <param name="recursive">If true, members are recursed to return descendant members as well
        ''' as immediate children. For example, a namespace would return the namespaces And types within.
        ''' However, if <paramref name="recursive"/> Is true, members with the namespaces And types would
        ''' also be returned.</param>
        ''' <param name="logicalFields">If true, field declarations are broken into their respective declarators.
        ''' For example, the field "Dim x, y As Integer" would return two nodes, one for x And one for y in place
        ''' of the field.</param>
        ''' <param name="onlySupportedNodes">If true, only members supported by Code Model are returned.</param>
        Public Overrides Iterator Function GetMemberNodes(container As SyntaxNode, includeSelf As Boolean, recursive As Boolean, logicalFields As Boolean, onlySupportedNodes As Boolean) As IEnumerable(Of SyntaxNode)

            If Not IsContainerNode(container) Then
                Exit Function
            End If

            If includeSelf AndAlso NodeIsSupported(onlySupportedNodes, container) Then
                Yield container
            End If

            For Each member In GetChildMemberNodes(container)

                If member.Kind = SyntaxKind.FieldDeclaration Then
                    ' For fields, the 'logical' and 'supported' flags are intrinsically tied.
                    '   * If 'logical' is true, only declarators should be returned, regardless of the value of 'supported'.
                    '   * If 'logical' is false, the field should only be returned if 'supported' is also false.

                    If logicalFields Then

                        For Each declarator In DirectCast(member, FieldDeclarationSyntax).Declarators

                            ' We know that declarators are supported, so there's no need to check them here.
                            For Each identifier In declarator.Names
                                Yield identifier
                            Next
                        Next

                    ElseIf Not onlySupportedNodes Then
                        ' Only return fields if the supported flag Is false.
                        Yield member
                    End If

                ElseIf NodeIsSupported(onlySupportedNodes, member) Then
                    Yield member
                End If

                If recursive AndAlso IsContainerNode(member) Then
                    For Each innerMember In GetMemberNodes(member, includeSelf:=False, recursive:=True, logicalFields:=logicalFields, onlySupportedNodes:=onlySupportedNodes)
                        Yield innerMember
                    Next
                End If
            Next
        End Function

        Public Overrides ReadOnly Property Language As String
            Get
                Return EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB
            End Get
        End Property

        Public Overrides ReadOnly Property AssemblyAttributeString As String
            Get
                Return "Assembly"
            End Get
        End Property

        ''' <summary>
        ''' Do not use this method directly! Instead, go through <see cref="FileCodeModel.GetOrCreateCodeElement(Of T)(SyntaxNode)"/>
        ''' </summary>
        Public Overloads Overrides Function CreateInternalCodeElement(
            state As CodeModelState,
            fileCodeModel As FileCodeModel,
            node As SyntaxNode
        ) As EnvDTE.CodeElement

            Select Case node.Kind
                Case SyntaxKind.Attribute
                    Return CType(CreateInternalCodeAttribute(state, fileCodeModel, node), EnvDTE.CodeElement)
                Case SyntaxKind.NameColonEquals
                    Return Nothing
                Case SyntaxKind.SimpleArgument
                    Return CType(CreateInternalCodeAttributeArgument(state, fileCodeModel, node), EnvDTE.CodeElement)
                Case SyntaxKind.SimpleImportsClause
                    Return CType(CreateInternalCodeImport(state, fileCodeModel, node), EnvDTE.CodeElement)
                Case SyntaxKind.ImportsStatement
                    Dim importsStatement = DirectCast(node, ImportsStatementSyntax)
                    Return CreateInternalCodeElement(state, fileCodeModel, importsStatement.ImportsClauses(0))
                Case SyntaxKind.Parameter
                    Return CType(CreateInternalCodeParameter(state, fileCodeModel, node), EnvDTE.CodeElement)
                Case SyntaxKind.OptionStatement
                    Return CType(CreateInternalCodeOptionStatement(state, fileCodeModel, node), EnvDTE.CodeElement)
                Case SyntaxKind.InheritsStatement
                    Return CType(CreateInternalCodeInheritsStatement(state, fileCodeModel, node), EnvDTE.CodeElement)
                Case SyntaxKind.ImplementsStatement
                    Return CType(CreateInternalCodeImplementsStatement(state, fileCodeModel, node), EnvDTE.CodeElement)
            End Select

            If IsAccessorNode(node) Then
                Return CType(CreateInternalCodeAccessorFunction(state, fileCodeModel, node), EnvDTE.CodeElement)
            End If

            Dim nodeKey = GetNodeKey(node)

            Select Case node.Kind
                Case SyntaxKind.ClassBlock,
                     SyntaxKind.ModuleBlock
                    Return CType(CodeClass.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case SyntaxKind.StructureBlock
                    Return CType(CodeStruct.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case SyntaxKind.InterfaceBlock
                    Return CType(CodeInterface.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case SyntaxKind.EnumBlock
                    Return CType(CodeEnum.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return CType(CodeDelegate.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case SyntaxKind.ConstructorBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement
                    Return CType(CodeFunctionWithEventHandler.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DeclareSubStatement
                    Return CType(CodeFunctionDeclareDecl.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case SyntaxKind.PropertyBlock,
                     SyntaxKind.PropertyStatement
                    Return CType(CodeProperty.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case SyntaxKind.EventBlock,
                     SyntaxKind.EventStatement
                    Return CType(CodeEvent.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case SyntaxKind.NamespaceBlock
                    Return CType(CodeNamespace.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case SyntaxKind.ModifiedIdentifier,
                     SyntaxKind.EnumMemberDeclaration
                    Return CType(CodeVariable.Create(state, fileCodeModel, nodeKey, node.Kind), EnvDTE.CodeElement)
                Case Else
                    Throw New NotImplementedException()
            End Select
        End Function

        Public Overrides Function CreateUnknownCodeElement(state As CodeModelState, fileCodeModel As FileCodeModel, node As SyntaxNode) As EnvDTE.CodeElement
            Select Case node.Kind
                Case SyntaxKind.ClassBlock,
                     SyntaxKind.ModuleBlock
                    Return CType(CodeClass.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.StructureBlock
                    Return CType(CodeStruct.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.InterfaceBlock
                    Return CType(CodeInterface.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.EnumBlock
                    Return CType(CodeEnum.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return CType(CodeDelegate.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.ConstructorBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement
                    Return CType(CodeFunctionWithEventHandler.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DeclareSubStatement
                    Return CType(CodeFunctionDeclareDecl.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.PropertyBlock,
                     SyntaxKind.PropertyStatement
                    Return CType(CodeProperty.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.EventBlock,
                     SyntaxKind.EventStatement
                    Return CType(CodeEvent.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.NamespaceBlock
                    Return CType(CodeNamespace.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.ModifiedIdentifier,
                     SyntaxKind.EnumMemberDeclaration
                    Return CType(CodeVariable.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.SimpleImportsClause
                    Return CType(CodeImport.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.OptionStatement
                    Return CType(CodeOptionsStatement.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.InheritsStatement
                    Return CType(CodeInheritsStatement.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)
                Case SyntaxKind.ImplementsStatement
                    Return CType(CodeImplementsStatement.CreateUnknown(state, fileCodeModel, node.Kind, GetName(node)), EnvDTE.CodeElement)

                Case Else
                    Throw New NotImplementedException()
            End Select
        End Function

        Public Overrides Function CreateUnknownRootNamespaceCodeElement(state As CodeModelState, fileCodeModel As FileCodeModel) As EnvDTE.CodeElement
            Dim compilation = CType(fileCodeModel.GetCompilation(), Compilation)
            Dim rootNamespace = DirectCast(compilation.Options, VisualBasicCompilationOptions).RootNamespace
            Return CType(CodeNamespace.CreateUnknown(state, fileCodeModel, SyntaxKind.NamespaceBlock, rootNamespace), EnvDTE.CodeElement)
        End Function

        Private Shared Function IsValidTypeRefKind(kind As EnvDTE.vsCMTypeRef) As Boolean
            Return kind = EnvDTE.vsCMTypeRef.vsCMTypeRefVoid OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefObject OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefBool OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefByte OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefShort OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefLong OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefDecimal OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefFloat OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefDouble OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefChar OrElse
                   kind = EnvDTE.vsCMTypeRef.vsCMTypeRefString OrElse
                   kind = EnvDTE80.vsCMTypeRef2.vsCMTypeRefSByte OrElse
                   kind = EnvDTE80.vsCMTypeRef2.vsCMTypeRefUnsignedInt OrElse
                   kind = EnvDTE80.vsCMTypeRef2.vsCMTypeRefUnsignedLong OrElse
                   kind = EnvDTE80.vsCMTypeRef2.vsCMTypeRefUnsignedShort
        End Function

        Public Overrides Function CreateCodeTypeRef(state As CodeModelState, projectId As ProjectId, type As Object) As EnvDTE.CodeTypeRef
            Dim project = state.Workspace.CurrentSolution.GetProject(projectId)
            If project Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim compilation = project.GetCompilationAsync().Result

            If TypeOf type Is Byte OrElse
               TypeOf type Is Short OrElse
               TypeOf type Is Integer Then

                Dim typeRefKind = CType(type, EnvDTE.vsCMTypeRef)
                If Not IsValidTypeRefKind(typeRefKind) Then
                    Throw Exceptions.ThrowEInvalidArg()
                End If

                Dim specialType = GetSpecialType(typeRefKind)
                Return CodeTypeRef.Create(state, Nothing, projectId, compilation.GetSpecialType(specialType))
            End If

            Dim typeName As String
            Dim parent As Object = Nothing

            If TypeOf type Is String Then
                typeName = CStr(type)
            ElseIf TypeOf type Is EnvDTE.CodeType Then
                typeName = CType(type, EnvDTE.CodeType).FullName
                parent = type
            Else
                Throw Exceptions.ThrowEInvalidArg()
            End If

            Dim typeSymbol = GetTypeSymbolFromFullName(typeName, compilation)
            If typeSymbol Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Return CodeTypeRef.Create(state, parent, projectId, typeSymbol)
        End Function

        Public Overrides Function GetTypeKindForCodeTypeRef(typeSymbol As ITypeSymbol) As EnvDTE.vsCMTypeRef
            ' Rough translation of CodeModelSymbol::GetTypeKind from vb\Language\VsPackage\CodeModelHelpers.cpp

            If typeSymbol.SpecialType = SpecialType.System_Void Then
                Return EnvDTE.vsCMTypeRef.vsCMTypeRefVoid
            End If

            If typeSymbol.TypeKind = TypeKind.Array Then
                Return EnvDTE.vsCMTypeRef.vsCMTypeRefArray
            End If

            If typeSymbol.TypeKind = TypeKind.Pointer Then
                typeSymbol = DirectCast(typeSymbol, IPointerTypeSymbol).PointedAtType
            End If

            If typeSymbol IsNot Nothing AndAlso Not typeSymbol.TypeKind = TypeKind.Error Then
                If typeSymbol.SpecialType = SpecialType.System_Object Then
                    Return EnvDTE.vsCMTypeRef.vsCMTypeRefObject
                End If

                If typeSymbol.TypeKind = TypeKind.Enum Then
                    Return EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                End If

                Select Case typeSymbol.SpecialType
                    Case SpecialType.System_Boolean
                        Return EnvDTE.vsCMTypeRef.vsCMTypeRefBool
                    Case SpecialType.System_SByte
                        Return CType(EnvDTE80.vsCMTypeRef2.vsCMTypeRefSByte, EnvDTE.vsCMTypeRef)
                    Case SpecialType.System_Byte
                        Return EnvDTE.vsCMTypeRef.vsCMTypeRefByte
                    Case SpecialType.System_Int16
                        Return EnvDTE.vsCMTypeRef.vsCMTypeRefShort
                    Case SpecialType.System_UInt16
                        Return CType(EnvDTE80.vsCMTypeRef2.vsCMTypeRefUnsignedShort, EnvDTE.vsCMTypeRef)
                    Case SpecialType.System_Int32
                        Return EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                    Case SpecialType.System_UInt32
                        Return CType(EnvDTE80.vsCMTypeRef2.vsCMTypeRefUnsignedInt, EnvDTE.vsCMTypeRef)
                    Case SpecialType.System_Int64
                        Return EnvDTE.vsCMTypeRef.vsCMTypeRefLong
                    Case SpecialType.System_UInt64
                        Return CType(EnvDTE80.vsCMTypeRef2.vsCMTypeRefUnsignedLong, EnvDTE.vsCMTypeRef)
                    Case SpecialType.System_Decimal
                        Return EnvDTE.vsCMTypeRef.vsCMTypeRefDecimal
                    Case SpecialType.System_Single
                        Return EnvDTE.vsCMTypeRef.vsCMTypeRefFloat
                    Case SpecialType.System_Double
                        Return EnvDTE.vsCMTypeRef.vsCMTypeRefDouble
                    Case SpecialType.System_Char
                        Return EnvDTE.vsCMTypeRef.vsCMTypeRefChar
                    Case SpecialType.System_String
                        Return EnvDTE.vsCMTypeRef.vsCMTypeRefString
                End Select

                If typeSymbol.TypeKind = TypeKind.Pointer Then
                    Return EnvDTE.vsCMTypeRef.vsCMTypeRefPointer
                End If

                If typeSymbol.TypeKind = TypeKind.TypeParameter Then
                    Return EnvDTE.vsCMTypeRef.vsCMTypeRefOther
                End If

                Return EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
            End If

            Return EnvDTE.vsCMTypeRef.vsCMTypeRefOther
        End Function

        Public Overrides Function GetAsFullNameForCodeTypeRef(typeSymbol As ITypeSymbol) As String
            Return typeSymbol.ToDisplayString(s_codeTypeRefAsFullNameFormat)
        End Function

        Public Overrides Function GetAsStringForCodeTypeRef(typeSymbol As ITypeSymbol) As String
            Return typeSymbol.ToDisplayString(s_codeTypeRefAsStringFormat)
        End Function

        Public Overrides Function IsParameterNode(node As SyntaxNode) As Boolean
            Return TypeOf node Is ParameterSyntax
        End Function

        Public Overrides Function IsAttributeNode(node As SyntaxNode) As Boolean
            Return TypeOf node Is AttributeSyntax
        End Function

        Public Overrides Function IsAttributeArgumentNode(node As SyntaxNode) As Boolean
            Return TypeOf node Is SimpleArgumentSyntax OrElse
                   TypeOf node Is OmittedArgumentSyntax
        End Function

        Public Overrides Function IsOptionNode(node As SyntaxNode) As Boolean
            Return TypeOf node Is OptionStatementSyntax
        End Function

        Public Overrides Function IsImportNode(node As SyntaxNode) As Boolean
            Return TypeOf node Is SimpleImportsClauseSyntax
        End Function

        Public Overrides Function GetUnescapedName(name As String) As String
            Return If(name IsNot Nothing AndAlso name.Length > 2 AndAlso name.StartsWith("[", StringComparison.Ordinal) AndAlso name.EndsWith("]", StringComparison.Ordinal),
                      name.Substring(1, name.Length - 2),
                      name)
        End Function

        Private Function GetNormalizedName(node As SyntaxNode) As String
            Dim nameBuilder = New StringBuilder()

            Dim token = node.GetFirstToken(includeSkipped:=True)
            While True
                nameBuilder.Append(token.ToString())

                Dim nextToken = token.GetNextToken(includeSkipped:=True)
                If Not nextToken.IsDescendantOf(node) Then
                    Exit While
                End If

                If (token.IsKeyword() OrElse token.Kind = SyntaxKind.IdentifierToken) AndAlso
                   (nextToken.IsKeyword() OrElse nextToken.Kind = SyntaxKind.IdentifierToken) Then

                    nameBuilder.Append(" "c)
                End If

                token = nextToken
            End While

            Return nameBuilder.ToString().Trim()
        End Function

        Public Overrides Function GetName(node As SyntaxNode) As String
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Debug.Assert(TypeOf node Is SyntaxNode)
            Debug.Assert(IsNameableNode(node))

            Select Case node.Kind
                Case SyntaxKind.Attribute
                    Return GetNormalizedName(DirectCast(node, AttributeSyntax).Name)
                Case SyntaxKind.ClassBlock,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.ModuleBlock,
                     SyntaxKind.StructureBlock
                    Return DirectCast(node, TypeBlockSyntax).BlockStatement.Identifier.ToString()
                Case SyntaxKind.EnumBlock
                    Return DirectCast(node, EnumBlockSyntax).EnumStatement.Identifier.ToString()
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(node, DelegateStatementSyntax).Identifier.ToString()
                Case SyntaxKind.NamespaceBlock
                    Return DirectCast(node, NamespaceBlockSyntax).NamespaceStatement.Name.ToString()
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock
                    Dim methodBlock = DirectCast(node, MethodBlockSyntax)
                    Return methodBlock.SubOrFunctionStatement.Identifier.ToString()
                Case SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement
                    Return DirectCast(node, MethodStatementSyntax).Identifier.ToString()
                Case SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DeclareSubStatement
                    Return DirectCast(node, DeclareStatementSyntax).Identifier.ToString()
                Case SyntaxKind.ConstructorBlock
                    Dim methodBlock = DirectCast(node, ConstructorBlockSyntax)
                    Return methodBlock.SubNewStatement.NewKeyword.ToString()
                Case SyntaxKind.OperatorBlock
                    Dim operatorBlock = DirectCast(node, OperatorBlockSyntax)
                    Return operatorBlock.OperatorStatement.OperatorToken.ToString()
                Case SyntaxKind.PropertyBlock
                    Dim propertyBlock = DirectCast(node, PropertyBlockSyntax)
                    Return propertyBlock.PropertyStatement.Identifier.ToString()
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(node, PropertyStatementSyntax).Identifier.ToString()
                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).EventStatement.Identifier.ToString()
                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).Identifier.ToString()
                Case SyntaxKind.ModifiedIdentifier
                    Return DirectCast(node, ModifiedIdentifierSyntax).Identifier.ToString()
                Case SyntaxKind.EnumMemberDeclaration
                    Return DirectCast(node, EnumMemberDeclarationSyntax).Identifier.ToString()
                Case SyntaxKind.SimpleArgument
                    Dim simpleArgument = DirectCast(node, SimpleArgumentSyntax)
                    Return If(simpleArgument.IsNamed,
                              simpleArgument.NameColonEquals.Name.ToString(),
                              String.Empty)
                Case SyntaxKind.OmittedArgument
                    Return String.Empty
                Case SyntaxKind.Parameter
                    Return GetParameterName(node)
                Case SyntaxKind.OptionStatement
                    Return GetNormalizedName(node)
                Case SyntaxKind.SimpleImportsClause
                    Return GetNormalizedName(DirectCast(node, ImportsClauseSyntax).GetName())
                Case SyntaxKind.InheritsStatement
                    Return DirectCast(node, InheritsStatementSyntax).InheritsKeyword.ToString()
                Case SyntaxKind.ImplementsStatement
                    Return DirectCast(node, ImplementsStatementSyntax).ImplementsKeyword.ToString()
                Case Else
                    Debug.Fail(String.Format("Invalid node kind: {0}", node.Kind))
                    Throw New ArgumentException()
            End Select
        End Function

        Public Overrides Function SetName(node As SyntaxNode, name As String) As SyntaxNode
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim identifier As SyntaxToken = SyntaxFactory.Identifier(name)

            Select Case node.Kind
                Case SyntaxKind.Attribute
                    Return DirectCast(node, AttributeSyntax).WithName(SyntaxFactory.ParseTypeName(name))
                Case SyntaxKind.ClassStatement
                    Return DirectCast(node, ClassStatementSyntax).WithIdentifier(identifier)
                Case SyntaxKind.InterfaceStatement
                    Return DirectCast(node, InterfaceStatementSyntax).WithIdentifier(identifier)
                Case SyntaxKind.ModuleStatement
                    Return DirectCast(node, ModuleStatementSyntax).WithIdentifier(identifier)
                Case SyntaxKind.StructureStatement
                    Return DirectCast(node, StructureStatementSyntax).WithIdentifier(identifier)
                Case SyntaxKind.EnumStatement
                    Return DirectCast(node, EnumStatementSyntax).WithIdentifier(identifier)
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(node, DelegateStatementSyntax).WithIdentifier(identifier)
                Case SyntaxKind.NamespaceStatement
                    Return DirectCast(node, NamespaceStatementSyntax).WithName(SyntaxFactory.ParseName(name))
                Case SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.SubNewStatement
                    Return DirectCast(node, MethodStatementSyntax).WithIdentifier(identifier)
                Case SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(node, DeclareStatementSyntax).WithIdentifier(identifier)
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(node, PropertyStatementSyntax).WithIdentifier(identifier)
                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).WithIdentifier(identifier)
                Case SyntaxKind.ModifiedIdentifier
                    Return DirectCast(node, ModifiedIdentifierSyntax).WithIdentifier(identifier)
                Case SyntaxKind.SimpleArgument
                    Return DirectCast(node, SimpleArgumentSyntax).WithNameColonEquals(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName(name)))
                Case Else
                    Debug.Fail("Invalid node kind: " & CType(node.Kind, SyntaxKind))
                    Throw Exceptions.ThrowEFail()
            End Select

        End Function

        Public Overrides Function GetNodeWithName(node As SyntaxNode) As SyntaxNode
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            If node.Kind = SyntaxKind.OperatorBlock Then
                Throw Exceptions.ThrowEFail
            End If

            Debug.Assert(IsNameableNode(node))

            Select Case node.Kind
                Case SyntaxKind.Attribute
                    Return node
                Case SyntaxKind.ClassBlock,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.ModuleBlock,
                     SyntaxKind.StructureBlock
                    Return DirectCast(node, TypeBlockSyntax).BlockStatement
                Case SyntaxKind.EnumBlock
                    Return DirectCast(node, EnumBlockSyntax).EnumStatement
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return node
                Case SyntaxKind.NamespaceBlock
                    Return DirectCast(node, NamespaceBlockSyntax).NamespaceStatement
                Case SyntaxKind.SubBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.ConstructorBlock
                    Return DirectCast(node, MethodBlockBaseSyntax).BlockStatement
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(node, PropertyBlockSyntax).PropertyStatement
                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).EventStatement
                Case SyntaxKind.ModifiedIdentifier
                    Return node
                Case SyntaxKind.SimpleArgument
                    Dim simpleArgument = DirectCast(node, SimpleArgumentSyntax)

                    Return If(simpleArgument.IsNamed, simpleArgument.NameColonEquals.Name, node)
                Case SyntaxKind.SubStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DeclareSubStatement
                    Return node
                Case SyntaxKind.EventStatement
                    Return node
                Case Else
                    Debug.Fail("Invalid node kind: " & CType(node.Kind, SyntaxKind))
                    Throw New ArgumentException()
            End Select
        End Function

        Public Overrides Function GetFullName(node As SyntaxNode, semanticModel As SemanticModel) As String
            If node.Kind = SyntaxKind.SimpleImportsClause Then
                Throw Exceptions.ThrowENotImpl()
            End If

            Dim symbol = If(TypeOf node Is AttributeSyntax,
                            semanticModel.GetTypeInfo(node).Type,
                            semanticModel.GetDeclaredSymbol(node))

            Return GetExternalSymbolFullName(symbol)
        End Function

        Public Overrides Function IsAccessorNode(node As SyntaxNode) As Boolean
            Select Case node.Kind
                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock

                    Return True
            End Select

            Return False
        End Function

        Public Overrides Function GetAccessorKind(node As SyntaxNode) As MethodKind
            Select Case node.Kind
                Case SyntaxKind.GetAccessorBlock
                    Return MethodKind.PropertyGet
                Case SyntaxKind.SetAccessorBlock
                    Return MethodKind.PropertySet
                Case SyntaxKind.AddHandlerAccessorBlock
                    Return MethodKind.EventAdd
                Case SyntaxKind.RemoveHandlerAccessorBlock
                    Return MethodKind.EventRemove
                Case SyntaxKind.RaiseEventAccessorBlock
                    Return MethodKind.EventRaise
                Case Else
                    Throw Exceptions.ThrowEUnexpected()
            End Select
        End Function

        Private Overloads Shared Function GetAccessorKind(methodKind As MethodKind) As SyntaxKind
            Select Case methodKind
                Case MethodKind.PropertyGet
                    Return SyntaxKind.GetAccessorBlock
                Case MethodKind.PropertySet
                    Return SyntaxKind.SetAccessorBlock
                Case MethodKind.EventAdd
                    Return SyntaxKind.AddHandlerAccessorBlock
                Case MethodKind.EventRemove
                    Return SyntaxKind.RemoveHandlerAccessorBlock
                Case MethodKind.EventRaise
                    Return SyntaxKind.RaiseEventAccessorBlock
                Case Else
                    Throw Exceptions.ThrowEUnexpected()
            End Select
        End Function

        Private Shared Function GetAccessors(node As SyntaxNode) As SyntaxList(Of AccessorBlockSyntax)
            Select Case node.Kind()
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(node, PropertyBlockSyntax).Accessors
                Case SyntaxKind.EventBlock
                    Return DirectCast(node, EventBlockSyntax).Accessors
                Case Else
                    Return Nothing
            End Select
        End Function

        Public Overrides Function TryGetAccessorNode(parentNode As SyntaxNode, kind As MethodKind, ByRef accessorNode As SyntaxNode) As Boolean
            Dim accessorKind = GetAccessorKind(kind)
            For Each accessor In GetAccessors(parentNode)
                If accessor.Kind = accessorKind Then
                    accessorNode = accessor
                    Return True
                End If
            Next

            accessorNode = Nothing
            Return False
        End Function

        Public Overrides Function TryGetParameterNode(parentNode As SyntaxNode, name As String, ByRef parameterNode As SyntaxNode) As Boolean
            For Each parameter As ParameterSyntax In GetParameterNodes(parentNode)
                Dim parameterName = GetNameFromParameter(parameter)
                If String.Equals(parameterName, name, StringComparison.OrdinalIgnoreCase) Then
                    parameterNode = parameter
                    Return True
                End If
            Next

            parameterNode = Nothing
            Return False
        End Function

        Private Overloads Function GetParameterNodes(methodStatement As MethodBaseSyntax) As IEnumerable(Of ParameterSyntax)
            Return If(methodStatement.ParameterList IsNot Nothing,
                      methodStatement.ParameterList.Parameters,
                      SpecializedCollections.EmptyEnumerable(Of ParameterSyntax))
        End Function

        Public Overloads Overrides Function GetParameterNodes(parent As SyntaxNode) As IEnumerable(Of SyntaxNode)
            If TypeOf parent Is MethodBaseSyntax Then
                Return GetParameterNodes(DirectCast(parent, MethodBaseSyntax))
            ElseIf TypeOf parent Is MethodBlockBaseSyntax Then
                Return GetParameterNodes(DirectCast(parent, MethodBlockBaseSyntax).BlockStatement)
            ElseIf TypeOf parent Is PropertyBlockSyntax Then
                Return GetParameterNodes(DirectCast(parent, PropertyBlockSyntax).PropertyStatement)
            End If

            Return SpecializedCollections.EmptyEnumerable(Of ParameterSyntax)()
        End Function

        Public Overrides Function TryGetImportNode(parentNode As SyntaxNode, dottedName As String, ByRef importNode As SyntaxNode) As Boolean
            For Each node In GetImportNodes(parentNode)
                If GetImportNamespaceOrType(node) = dottedName Then
                    importNode = node
                    Return True
                End If
            Next

            importNode = Nothing
            Return False
        End Function

        Public Overrides Function TryGetOptionNode(parentNode As SyntaxNode, name As String, ordinal As Integer, ByRef optionNode As SyntaxNode) As Boolean
            Dim count = -1
            For Each [option] As OptionStatementSyntax In GetOptionNodes(parentNode)
                If [option].ToString() = name Then
                    count += 1
                    If count = ordinal Then
                        optionNode = [option]
                        Return True
                    End If
                End If
            Next

            optionNode = Nothing
            Return False
        End Function

        Public Overrides Function TryGetInheritsNode(parentNode As SyntaxNode, name As String, ordinal As Integer, ByRef inheritsNode As SyntaxNode) As Boolean
            Dim count = -1
            For Each [inherits] As InheritsStatementSyntax In GetInheritsNodes(parentNode)
                If [inherits].Types.ToString() = name Then
                    count += 1
                    If count = ordinal Then
                        inheritsNode = [inherits]
                        Return True
                    End If
                End If
            Next

            inheritsNode = Nothing
            Return False
        End Function

        Public Overrides Function TryGetImplementsNode(parentNode As SyntaxNode, name As String, ordinal As Integer, ByRef implementsNode As SyntaxNode) As Boolean
            Dim count = -1
            For Each [implements] As ImplementsStatementSyntax In GetImplementsNodes(parentNode)
                If [implements].Types.ToString() = name Then
                    count += 1
                    If count = ordinal Then
                        implementsNode = [implements]
                        Return True
                    End If
                End If
            Next

            implementsNode = Nothing
            Return False
        End Function

        Public Overrides Function TryGetAttributeNode(parentNode As SyntaxNode, name As String, ordinal As Integer, ByRef attributeNode As SyntaxNode) As Boolean
            Dim count = -1
            For Each attribute As AttributeSyntax In GetAttributeNodes(parentNode)
                If attribute.Name.ToString() = name Then
                    count += 1
                    If count = ordinal Then
                        attributeNode = attribute
                        Return True
                    End If
                End If
            Next

            attributeNode = Nothing
            Return False
        End Function

        Public Overrides Function TryGetAttributeArgumentNode(attributeNode As SyntaxNode, index As Integer, ByRef attributeArgumentNode As SyntaxNode) As Boolean
            Debug.Assert(TypeOf attributeNode Is AttributeSyntax)

            Dim attribute = DirectCast(attributeNode, AttributeSyntax)
            If attribute.ArgumentList IsNot Nothing AndAlso
                attribute.ArgumentList.Arguments.Count > index Then

                attributeArgumentNode = attribute.ArgumentList.Arguments(index)
                Return True
            End If

            attributeArgumentNode = Nothing
            Return False
        End Function

        Private Function DeleteMember(document As Document, node As SyntaxNode) As Document
            Dim text = document.GetTextAsync(CancellationToken.None) _
                               .WaitAndGetResult(CancellationToken.None)

            Dim deletionEnd = node.FullSpan.End
            Dim deletionStart = node.SpanStart
            Dim contiguousEndOfLines = 0

            For Each trivia In node.GetLeadingTrivia().Reverse()
                If trivia.IsDirective Then
                    Exit For
                End If

                If trivia.Kind = SyntaxKind.EndOfLineTrivia Then
                    If contiguousEndOfLines > 0 Then
                        Exit For
                    Else
                        contiguousEndOfLines += 1
                    End If
                ElseIf trivia.Kind <> SyntaxKind.WhitespaceTrivia Then
                    contiguousEndOfLines = 0
                End If

                deletionStart = trivia.FullSpan.Start
            Next

            text = text.Replace(TextSpan.FromBounds(deletionStart, deletionEnd), String.Empty)

            Return document.WithText(text)
        End Function

        Public Overrides Function Delete(document As Document, node As SyntaxNode) As Document
            Select Case node.Kind
                Case SyntaxKind.Attribute
                    Return Delete(document, DirectCast(node, AttributeSyntax))
                Case SyntaxKind.SimpleArgument
                    Return Delete(document, DirectCast(node, ArgumentSyntax))
                Case SyntaxKind.Parameter
                    Return Delete(document, DirectCast(node, ParameterSyntax))
                Case SyntaxKind.ModifiedIdentifier
                    Return Delete(document, DirectCast(node, ModifiedIdentifierSyntax))
                Case SyntaxKind.VariableDeclarator
                    Return Delete(document, DirectCast(node, VariableDeclaratorSyntax))
                Case Else
                    Return DeleteMember(document, node)
            End Select
        End Function

        Private Overloads Function Delete(document As Document, node As ModifiedIdentifierSyntax) As Document
            Dim declarator = node.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()

            ' If this is the only name in declarator, then delete the entire
            ' declarator.
            If declarator.Names.Count = 1 Then
                Return Delete(document, declarator)
            Else
                Dim newDeclarator = declarator.RemoveNode(node, SyntaxRemoveOptions.KeepEndOfLine).WithAdditionalAnnotations(Formatter.Annotation)
                Return document.ReplaceNodeAsync(declarator, newDeclarator, CancellationToken.None).WaitAndGetResult(CancellationToken.None)
            End If
        End Function

        Private Overloads Function Delete(document As Document, node As VariableDeclaratorSyntax) As Document
            Dim declaration = node.FirstAncestorOrSelf(Of FieldDeclarationSyntax)()

            ' If this is the only declarator in the declaration, then delete
            ' the entire declarator.
            If declaration.Declarators.Count = 1 Then
                Return Delete(document, declaration)
            Else
                Dim newDeclaration = declaration.RemoveNode(node, SyntaxRemoveOptions.KeepEndOfLine).WithAdditionalAnnotations(Formatter.Annotation)
                Return document.ReplaceNodeAsync(declaration, newDeclaration, CancellationToken.None).WaitAndGetResult(CancellationToken.None)
            End If
        End Function

        Private Overloads Function Delete(document As Document, node As AttributeSyntax) As Document
            Dim attributeList = node.FirstAncestorOrSelf(Of AttributeListSyntax)()

            ' If we don't have anything left, then just delete the whole attribute list.
            ' Keep all leading trivia, but delete all trailing trivia.
            If attributeList.Attributes.Count = 1 Then
                Dim spanStart = attributeList.SpanStart
                Dim spanEnd = attributeList.FullSpan.End

                Dim text = document.GetTextAsync(CancellationToken.None) _
                                   .WaitAndGetResult(CancellationToken.None)

                text = text.Replace(TextSpan.FromBounds(spanStart, spanEnd), String.Empty)

                Return document.WithText(text)
            Else
                Dim newAttributeList = attributeList.RemoveNode(node, SyntaxRemoveOptions.KeepEndOfLine)

                Return document.ReplaceNodeAsync(attributeList, newAttributeList, CancellationToken.None) _
                               .WaitAndGetResult(CancellationToken.None)
            End If
        End Function

        Private Overloads Function Delete(document As Document, node As ArgumentSyntax) As Document
            Dim argumentList = node.FirstAncestorOrSelf(Of ArgumentListSyntax)()
            Dim newArgumentList = argumentList.RemoveNode(node, SyntaxRemoveOptions.KeepEndOfLine).WithAdditionalAnnotations(Formatter.Annotation)

            Return document.ReplaceNodeAsync(argumentList, newArgumentList, CancellationToken.None) _
                           .WaitAndGetResult(CancellationToken.None)
        End Function

        Private Overloads Function Delete(document As Document, node As ParameterSyntax) As Document
            Dim parameterList = node.FirstAncestorOrSelf(Of ParameterListSyntax)()
            Dim newParameterList = parameterList.RemoveNode(node, SyntaxRemoveOptions.KeepEndOfLine).WithAdditionalAnnotations(Formatter.Annotation)

            Return document.ReplaceNodeAsync(parameterList, newParameterList, CancellationToken.None) _
                           .WaitAndGetResult(CancellationToken.None)
        End Function

        Public Overrides Function IsValidExternalSymbol(symbol As ISymbol) As Boolean
            Dim methodSymbol = TryCast(symbol, IMethodSymbol)
            If methodSymbol IsNot Nothing Then
                If methodSymbol.MethodKind = MethodKind.PropertyGet OrElse
                   methodSymbol.MethodKind = MethodKind.PropertySet OrElse
                   methodSymbol.MethodKind = MethodKind.EventAdd OrElse
                   methodSymbol.MethodKind = MethodKind.EventRemove OrElse
                   methodSymbol.MethodKind = MethodKind.EventRaise Then

                    Return False
                End If
            End If

            Dim fieldSymbol = TryCast(symbol, IFieldSymbol)
            If fieldSymbol IsNot Nothing Then
                Dim propertySymbol = TryCast(fieldSymbol.AssociatedSymbol, IPropertySymbol)
                If propertySymbol?.IsWithEvents Then
                    Return True
                End If
            End If

            Return symbol.DeclaredAccessibility = Accessibility.Public OrElse
                   symbol.DeclaredAccessibility = Accessibility.Protected OrElse
                   symbol.DeclaredAccessibility = Accessibility.ProtectedOrFriend OrElse
                   symbol.DeclaredAccessibility = Accessibility.Friend
        End Function

        Public Overrides Function GetExternalSymbolName(symbol As ISymbol) As String
            If symbol Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Return symbol.ToDisplayString(s_externalNameFormat)
        End Function

        Public Overrides Function GetExternalSymbolFullName(symbol As ISymbol) As String
            If symbol Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Return symbol.ToDisplayString(s_externalfullNameFormat)
        End Function

        Public Overrides Function GetAccess(symbol As ISymbol) As EnvDTE.vsCMAccess
            Debug.Assert(symbol IsNot Nothing)

            Dim access As EnvDTE.vsCMAccess = 0

            Select Case symbol.DeclaredAccessibility
                Case Accessibility.Private
                    access = access Or EnvDTE.vsCMAccess.vsCMAccessPrivate
                Case Accessibility.Protected
                    access = access Or EnvDTE.vsCMAccess.vsCMAccessProtected
                Case Accessibility.Internal, Accessibility.Friend
                    access = access Or EnvDTE.vsCMAccess.vsCMAccessProject
                Case Accessibility.ProtectedOrInternal, Accessibility.ProtectedOrFriend
                    access = access Or EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected
                Case Accessibility.Public
                    access = access Or EnvDTE.vsCMAccess.vsCMAccessPublic
                Case Else
                    Throw Exceptions.ThrowEFail()
            End Select

            If TryCast(symbol, IPropertySymbol)?.IsWithEvents Then
                access = access Or EnvDTE.vsCMAccess.vsCMAccessWithEvents
            End If

            Return access
        End Function

        Public Overrides Function GetAccess(node As SyntaxNode) As EnvDTE.vsCMAccess
            Dim member = TryCast(Me.GetNodeWithModifiers(node), StatementSyntax)

            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim flags = member.GetModifierFlags()

            Dim access As EnvDTE.vsCMAccess = 0

            If (flags And ModifierFlags.Public) <> 0 Then
                access = EnvDTE.vsCMAccess.vsCMAccessPublic
            ElseIf (flags And ModifierFlags.Protected) <> 0 AndAlso
                   (flags And ModifierFlags.Friend) <> 0 Then
                access = EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected
            ElseIf (flags And ModifierFlags.Friend) <> 0 Then
                access = EnvDTE.vsCMAccess.vsCMAccessProject
            ElseIf (flags And ModifierFlags.Protected) <> 0 Then
                access = EnvDTE.vsCMAccess.vsCMAccessProtected
            ElseIf (flags And ModifierFlags.Private) <> 0 Then
                access = EnvDTE.vsCMAccess.vsCMAccessPrivate
            Else
                ' The code does not specify the accessibility, so we need to
                ' determine the default accessibility
                access = GetDefaultAccessibility(member)
            End If

            If (flags And ModifierFlags.WithEvents) <> 0 Then
                access = access Or EnvDTE.vsCMAccess.vsCMAccessWithEvents
            End If

            Return access
        End Function

        Public Overrides Function GetNodeWithModifiers(node As SyntaxNode) As SyntaxNode
            Return If(TypeOf node Is ModifiedIdentifierSyntax,
                      node.GetAncestor(Of DeclarationStatementSyntax)(),
                      node)
        End Function

        Public Overrides Function GetNodeWithType(node As SyntaxNode) As SyntaxNode
            Return If(TypeOf node Is ModifiedIdentifierSyntax,
                      node.GetAncestor(Of VariableDeclaratorSyntax)(),
                      node)
        End Function

        Public Overrides Function GetNodeWithInitializer(node As SyntaxNode) As SyntaxNode
            Return If(TypeOf node Is ModifiedIdentifierSyntax,
                      node.GetAncestor(Of VariableDeclaratorSyntax)(),
                      node)
        End Function

        Public Overrides Function SetAccess(node As SyntaxNode, newAccess As EnvDTE.vsCMAccess) As SyntaxNode
            Dim member = TryCast(node, StatementSyntax)

            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            If member.Parent.Kind = SyntaxKind.InterfaceBlock OrElse
                member.Parent.Kind = SyntaxKind.EnumBlock Then
                If newAccess = EnvDTE.vsCMAccess.vsCMAccessDefault OrElse
                    newAccess = EnvDTE.vsCMAccess.vsCMAccessPublic Then
                    Return node
                Else
                    Throw Exceptions.ThrowEInvalidArg()
                End If
            End If

            If TypeOf member Is TypeBlockSyntax OrElse
                TypeOf member Is EnumBlockSyntax Then
                If Not TypeOf member.Parent Is TypeBlockSyntax AndAlso
                    (newAccess = EnvDTE.vsCMAccess.vsCMAccessPrivate OrElse
                     newAccess = EnvDTE.vsCMAccess.vsCMAccessProtected OrElse
                     newAccess = EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected) Then
                    Throw Exceptions.ThrowEInvalidArg()
                End If
            End If

            Dim flags = member.GetModifierFlags() And Not (ModifierFlags.AccessModifierMask Or ModifierFlags.Dim Or ModifierFlags.WithEvents)

            If (newAccess And EnvDTE.vsCMAccess.vsCMAccessPrivate) <> 0 Then
                flags = flags Or ModifierFlags.Private
            ElseIf (newAccess And EnvDTE.vsCMAccess.vsCMAccessProtected) <> 0 Then
                flags = flags Or ModifierFlags.Protected

                If (newAccess And EnvDTE.vsCMAccess.vsCMAccessProject) <> 0 Then
                    flags = flags Or ModifierFlags.Friend
                End If
            ElseIf (newAccess And EnvDTE.vsCMAccess.vsCMAccessPublic) <> 0 Then
                flags = flags Or ModifierFlags.Public
            ElseIf (newAccess And EnvDTE.vsCMAccess.vsCMAccessProject) <> 0 Then
                flags = flags Or ModifierFlags.Friend
            ElseIf (newAccess And EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected) <> 0 Then
                flags = flags Or ModifierFlags.Protected Or ModifierFlags.Friend
            ElseIf (newAccess And EnvDTE.vsCMAccess.vsCMAccessDefault) <> 0 Then
                ' No change
            End If

            If (newAccess And EnvDTE.vsCMAccess.vsCMAccessWithEvents) <> 0 Then
                flags = flags Or ModifierFlags.WithEvents
            End If

            If flags = 0 AndAlso member.IsKind(SyntaxKind.FieldDeclaration) Then
                flags = flags Or ModifierFlags.Dim
            End If

            Return member.UpdateModifiers(flags)
        End Function

        Private Overloads Function GetDefaultAccessibility(node As SyntaxNode) As EnvDTE.vsCMAccess
            If node.HasAncestor(Of StructureBlockSyntax)() Then
                Return EnvDTE.vsCMAccess.vsCMAccessPublic
            End If

            If TypeOf node Is FieldDeclarationSyntax Then
                Return EnvDTE.vsCMAccess.vsCMAccessPrivate
            ElseIf (TypeOf node Is MethodBlockBaseSyntax OrElse
                    TypeOf node Is TypeBlockSyntax OrElse
                    TypeOf node Is EnumBlockSyntax OrElse
                    TypeOf node Is MethodBaseSyntax OrElse
                    TypeOf node Is EnumMemberDeclarationSyntax) Then
                Return EnvDTE.vsCMAccess.vsCMAccessPublic
            End If

            Throw Exceptions.ThrowEFail()
        End Function

        Protected Overrides Function GetAttributeIndexInContainer(containerNode As SyntaxNode, predicate As Func(Of SyntaxNode, Boolean)) As Integer
            Dim attributes = GetAttributeNodes(containerNode).ToArray()

            Dim index = 0
            While index < attributes.Length
                Dim attribute = DirectCast(attributes(index), AttributeSyntax)

                If predicate(attribute) Then
                    Dim attributeBlock = DirectCast(attribute.Parent, AttributeListSyntax)

                    ' If this attribute is part of a block with multiple attributes,
                    ' make sure to return the index of the last attribute in the block.
                    If attributeBlock.Attributes.Count > 1 Then
                        Dim indexOfAttributeInBlock = attributeBlock.Attributes.IndexOf(attribute)
                        Return index + (attributeBlock.Attributes.Count - indexOfAttributeInBlock)
                    End If

                    Return index + 1
                End If

                index += 1
            End While

            Return -1
        End Function

        Protected Overrides Function GetAttributeArgumentIndexInContainer(containerNode As SyntaxNode, predicate As Func(Of SyntaxNode, Boolean)) As Integer
            Dim attributeArguments = GetAttributeArgumentNodes(containerNode).ToArray()

            Dim index = 0
            While index < attributeArguments.Length
                If predicate(attributeArguments(index)) Then
                    Return index + 1
                End If

                index += 1
            End While

            Return -1
        End Function

        Protected Overrides Function GetImportIndexInContainer(containerNode As SyntaxNode, predicate As Func(Of SyntaxNode, Boolean)) As Integer
            Dim importsClauses = GetImportNodes(containerNode).ToArray()

            Dim index = 0
            While index < importsClauses.Length
                Dim importsClause = DirectCast(importsClauses(index), ImportsClauseSyntax)

                If predicate(importsClause) Then
                    Dim importsStatement = DirectCast(importsClause.Parent, ImportsStatementSyntax)

                    ' If this attribute is part of a block with multiple attributes,
                    ' make sure to return the index of the last attribute in the block.
                    If importsStatement.ImportsClauses.Count > 1 Then
                        Dim indexOfImportClauseInStatement = importsStatement.ImportsClauses.IndexOf(importsClause)
                        Return index + (importsStatement.ImportsClauses.Count - indexOfImportClauseInStatement)
                    End If

                    Return index + 1
                End If

                index += 1
            End While

            Return -1
        End Function

        Protected Overrides Function GetParameterIndexInContainer(containerNode As SyntaxNode, predicate As Func(Of SyntaxNode, Boolean)) As Integer
            Dim parameters = GetParameterNodes(containerNode).ToArray()

            For index = 0 To parameters.Length - 1
                If predicate(parameters(index)) Then
                    Return index + 1
                End If
            Next

            Return -1
        End Function

        Protected Overrides Function GetMemberIndexInContainer(containerNode As SyntaxNode, predicate As Func(Of SyntaxNode, Boolean)) As Integer
            Dim members = GetLogicalMemberNodes(containerNode).ToArray()

            Dim index = 0
            While index < members.Length
                Dim member = members(index)
                If predicate(member) Then
                    ' Special case: if a modified identifier was specified, make sure we return the index
                    ' of the last modified identifier of the last variable declarator in the parenting field
                    ' declaration.
                    If member.Kind = SyntaxKind.ModifiedIdentifier Then
                        Dim modifiedIdentifier = DirectCast(member, ModifiedIdentifierSyntax)
                        Dim variableDeclarator = DirectCast(member.Parent, VariableDeclaratorSyntax)
                        Dim fieldDeclaration = DirectCast(variableDeclarator.Parent, FieldDeclarationSyntax)

                        Dim indexOfNameInDeclarator = variableDeclarator.Names.IndexOf(modifiedIdentifier)
                        Dim indexOfDeclaratorInField = fieldDeclaration.Declarators.IndexOf(variableDeclarator)

                        Dim indexOfNameInField = indexOfNameInDeclarator
                        If indexOfDeclaratorInField > 0 Then
                            For i = 0 To indexOfDeclaratorInField - 1
                                indexOfNameInField += fieldDeclaration.Declarators(i).Names.Count
                            Next
                        End If

                        Dim namesInFieldCount = fieldDeclaration.Declarators.SelectMany(Function(v) v.Names).Count()

                        Return index + (namesInFieldCount - indexOfNameInField)
                    End If

                    Return index + 1
                End If

                index += 1
            End While

            Return -1
        End Function

        Public Overrides Sub GetOptionNameAndOrdinal(parentNode As SyntaxNode, optionNode As SyntaxNode, ByRef name As String, ByRef ordinal As Integer)
            Debug.Assert(TypeOf optionNode Is OptionStatementSyntax)

            name = GetNormalizedName(DirectCast(optionNode, OptionStatementSyntax))

            ordinal = -1
            For Each [option] As OptionStatementSyntax In GetOptionNodes(parentNode)
                If GetNormalizedName([option]) = name Then
                    ordinal += 1
                End If

                If [option].Equals(optionNode) Then
                    Exit For
                End If
            Next
        End Sub

        Public Overrides Sub GetInheritsNamespaceAndOrdinal(parentNode As SyntaxNode, inheritsNode As SyntaxNode, ByRef namespaceName As String, ByRef ordinal As Integer)
            Debug.Assert(TypeOf inheritsNode Is InheritsStatementSyntax)

            namespaceName = DirectCast(inheritsNode, InheritsStatementSyntax).Types.ToString()

            ordinal = -1
            For Each [inherits] As InheritsStatementSyntax In GetInheritsNodes(parentNode)
                If [inherits].Types.ToString() = namespaceName Then
                    ordinal += 1
                End If

                If [inherits].Equals(inheritsNode) Then
                    Exit For
                End If
            Next
        End Sub

        Public Overrides Sub GetImplementsNamespaceAndOrdinal(parentNode As SyntaxNode, implementsNode As SyntaxNode, ByRef namespaceName As String, ByRef ordinal As Integer)
            Debug.Assert(TypeOf implementsNode Is ImplementsStatementSyntax)

            namespaceName = DirectCast(implementsNode, ImplementsStatementSyntax).Types.ToString()

            ordinal = -1
            For Each [implements] As ImplementsStatementSyntax In GetImplementsNodes(parentNode)
                If [implements].Types.ToString() = namespaceName Then
                    ordinal += 1
                End If

                If [implements].Equals(implementsNode) Then
                    Exit For
                End If
            Next
        End Sub

        Public Overrides Sub GetAttributeNameAndOrdinal(parentNode As SyntaxNode, attributeNode As SyntaxNode, ByRef name As String, ByRef ordinal As Integer)
            Debug.Assert(TypeOf attributeNode Is AttributeSyntax)

            name = DirectCast(attributeNode, AttributeSyntax).Name.ToString()

            ordinal = -1
            For Each attribute As AttributeSyntax In GetAttributeNodes(parentNode)
                If attribute.Name.ToString() = name Then
                    ordinal += 1
                End If

                If attribute.Equals(attributeNode) Then
                    Exit For
                End If
            Next
        End Sub

        Public Overrides Function GetAttributeTargetNode(attributeNode As SyntaxNode) As SyntaxNode
            If TypeOf attributeNode Is AttributeSyntax Then
                Return attributeNode
            End If

            Throw Exceptions.ThrowEUnexpected()
        End Function

        Public Overrides Function GetAttributeTarget(attributeNode As SyntaxNode) As String
            Debug.Assert(TypeOf attributeNode Is AttributeSyntax)

            Dim attribute = DirectCast(attributeNode, AttributeSyntax)

            Return If(attribute.Target IsNot Nothing,
                      attribute.Target.AttributeModifier.ToString(),
                      String.Empty)
        End Function

        Public Overrides Function SetAttributeTarget(attributeNode As SyntaxNode, value As String) As SyntaxNode
            Debug.Assert(TypeOf attributeNode Is AttributeSyntax)

            Dim attribute = DirectCast(attributeNode, AttributeSyntax)
            Dim target = attribute.Target

            If Not String.IsNullOrEmpty(value) Then
                ' VB only supports Assembly and Module as attribute modifiers.
                Dim newModifier As SyntaxToken
                If String.Equals(value, "Assembly", StringComparison.OrdinalIgnoreCase) Then
                    newModifier = SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)
                ElseIf String.Equals(value, "Module", StringComparison.OrdinalIgnoreCase) Then
                    newModifier = SyntaxFactory.Token(SyntaxKind.ModuleKeyword)
                Else
                    Throw Exceptions.ThrowEInvalidArg()
                End If

                Dim newTarget = If(target IsNot Nothing,
                                   target.WithAttributeModifier(newModifier),
                                   SyntaxFactory.AttributeTarget(newModifier))

                Return attribute.WithTarget(newTarget)
            Else
                Return attribute.WithTarget(Nothing)
            End If
        End Function

        Public Overrides Function GetAttributeValue(attributeNode As SyntaxNode) As String
            Debug.Assert(TypeOf attributeNode Is AttributeSyntax)

            Dim attribute = DirectCast(attributeNode, AttributeSyntax)
            Dim argumentList = attribute.ArgumentList
            If argumentList IsNot Nothing Then
                Return argumentList.Arguments.ToString()
            End If

            Return String.Empty
        End Function

        Public Overrides Function SetAttributeValue(attributeNode As SyntaxNode, value As String) As SyntaxNode
            Debug.Assert(TypeOf attributeNode Is AttributeSyntax)

            Dim attribute = DirectCast(attributeNode, AttributeSyntax)
            Dim argumentList = attribute.ArgumentList

            Dim parsedArgumentList = SyntaxFactory.ParseArgumentList("(" & value & ")")
            Dim newArgumentList = If(argumentList IsNot Nothing,
                                     argumentList.WithArguments(parsedArgumentList.Arguments),
                                     parsedArgumentList)

            Return attribute.WithArgumentList(newArgumentList)
        End Function

        Public Overrides Function GetNodeWithAttributes(node As SyntaxNode) As SyntaxNode
            Return If(TypeOf node Is ModifiedIdentifierSyntax,
                      node.GetAncestor(Of FieldDeclarationSyntax),
                      node)
        End Function

        Public Overrides Function GetEffectiveParentForAttribute(node As SyntaxNode) As SyntaxNode
            If node.HasAncestor(Of FieldDeclarationSyntax)() Then
                Return node.GetAncestor(Of FieldDeclarationSyntax).Declarators.First().Names.First()
            ElseIf node.HasAncestor(Of ParameterSyntax)() Then
                Return node.GetAncestor(Of ParameterSyntax)()
            Else
                Return node.Parent
            End If
        End Function

        Public Overrides Sub GetAttributeArgumentParentAndIndex(attributeArgumentNode As SyntaxNode, ByRef attributeNode As SyntaxNode, ByRef index As Integer)
            Debug.Assert(TypeOf attributeArgumentNode Is ArgumentSyntax)

            Dim argument = DirectCast(attributeArgumentNode, ArgumentSyntax)
            Dim attribute = DirectCast(argument.Ancestors.FirstOrDefault(Function(n) n.Kind = SyntaxKind.Attribute), AttributeSyntax)


            attributeNode = attribute
            index = attribute.ArgumentList.Arguments.IndexOf(DirectCast(attributeArgumentNode, ArgumentSyntax))
        End Sub

        Public Overrides Function CreateAttributeNode(name As String, value As String, Optional target As String = Nothing) As SyntaxNode
            Dim specifier As AttributeTargetSyntax = Nothing
            If target IsNot Nothing Then
                Dim contextualKeywordKind = SyntaxFacts.GetContextualKeywordKind(target)
                If contextualKeywordKind = SyntaxKind.AssemblyKeyword OrElse
                    contextualKeywordKind = SyntaxKind.ModuleKeyword Then
                    specifier = SyntaxFactory.AttributeTarget(SyntaxFactory.Token(contextualKeywordKind, text:=target))
                Else
                    specifier = SyntaxFactory.AttributeTarget(SyntaxFactory.ParseToken(target))
                End If
            End If

            Return SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                        target:=specifier,
                        name:=SyntaxFactory.ParseName(name),
                        argumentList:=SyntaxFactory.ParseArgumentList("(" & value & ")"))))

        End Function

        Public Overrides Function CreateAttributeArgumentNode(name As String, value As String) As SyntaxNode
            If Not String.IsNullOrEmpty(name) Then
                Return SyntaxFactory.SimpleArgument(
                           SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName(name)),
                           SyntaxFactory.ParseExpression(value))
            Else
                Return SyntaxFactory.SimpleArgument(SyntaxFactory.ParseExpression(value))
            End If
        End Function

        Public Overrides Function CreateImportNode(name As String, Optional [alias] As String = Nothing) As SyntaxNode
            Dim nameSyntax = SyntaxFactory.ParseName(name)

            Dim importsClause As ImportsClauseSyntax
            If Not String.IsNullOrEmpty([alias]) Then
                importsClause = SyntaxFactory.SimpleImportsClause(SyntaxFactory.ImportAliasClause([alias]), nameSyntax)
            Else
                importsClause = SyntaxFactory.SimpleImportsClause(nameSyntax)
            End If

            Return SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList(importsClause))
        End Function

        Public Overrides Function CreateParameterNode(name As String, type As String) As SyntaxNode
            Return SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier(name)).WithAsClause(SyntaxFactory.SimpleAsClause(SyntaxFactory.ParseTypeName(type)))
        End Function

        Public Overrides Function GetAttributeArgumentValue(attributeArgumentNode As SyntaxNode) As String
            Select Case attributeArgumentNode.Kind
                Case SyntaxKind.SimpleArgument
                    Return DirectCast(attributeArgumentNode, SimpleArgumentSyntax).Expression.ToString()
            End Select

            Throw New InvalidOperationException()
        End Function

        Public Overrides Function GetImportAlias(node As SyntaxNode) As String
            Select Case node.Kind
                Case SyntaxKind.SimpleImportsClause
                    Dim simpleImportsClause = DirectCast(node, SimpleImportsClauseSyntax)
                    Return If(simpleImportsClause.Alias IsNot Nothing,
                              simpleImportsClause.Alias.Identifier.ToString(),
                              String.Empty)
                Case Else
                    Throw New InvalidOperationException()
            End Select

        End Function

        Public Overrides Function GetImportNamespaceOrType(node As SyntaxNode) As String
            Select Case node.Kind
                Case SyntaxKind.SimpleImportsClause
                    Return GetNormalizedName(DirectCast(node, SimpleImportsClauseSyntax).Name)
                Case Else
                    Throw New InvalidOperationException()
            End Select
        End Function

        Public Overrides Sub GetImportParentAndName(node As SyntaxNode, ByRef parentNode As SyntaxNode, ByRef name As String)
            parentNode = Nothing

            Select Case node.Kind
                Case SyntaxKind.SimpleImportsClause
                    name = GetNormalizedName(DirectCast(node, SimpleImportsClauseSyntax).Name)
                Case Else
                    Throw New InvalidOperationException()
            End Select
        End Sub

        Public Overrides Function GetParameterName(node As SyntaxNode) As String
            Dim parameter = TryCast(node, ParameterSyntax)
            If parameter IsNot Nothing Then
                Return GetNameFromParameter(parameter)
            End If

            Throw New InvalidOperationException()
        End Function

        Private Function GetNameFromParameter(parameter As ParameterSyntax) As String
            Dim parameterName As String = parameter.Identifier.Identifier.ToString()
            Return If(Not String.IsNullOrEmpty(parameterName) AndAlso SyntaxFactsService.IsTypeCharacter(parameterName.Last()),
                parameterName.Substring(0, parameterName.Length - 1),
                parameterName)
        End Function

        Public Overrides Function GetParameterFullName(node As SyntaxNode) As String
            Dim parameter = TryCast(node, ParameterSyntax)
            If parameter IsNot Nothing Then
                Return parameter.Identifier.ToString()
            End If

            Throw New InvalidOperationException()
        End Function

        Public Overrides Function GetParameterKind(node As SyntaxNode) As EnvDTE80.vsCMParameterKind
            Dim parameter = TryCast(node, ParameterSyntax)
            If parameter IsNot Nothing Then
                Dim kind = EnvDTE80.vsCMParameterKind.vsCMParameterKindNone

                Dim modifiers = parameter.Modifiers

                If modifiers.Any(SyntaxKind.OptionalKeyword) Then
                    kind = kind Or EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional
                End If

                If modifiers.Any(SyntaxKind.ParamArrayKeyword) Then
                    kind = kind Or EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray
                End If

                If modifiers.Any(SyntaxKind.ByRefKeyword) Then
                    kind = kind Or EnvDTE80.vsCMParameterKind.vsCMParameterKindRef
                Else
                    kind = kind Or EnvDTE80.vsCMParameterKind.vsCMParameterKindIn
                End If

                Return kind

            End If

            Throw New InvalidOperationException()
        End Function

        Public Overrides Function SetParameterKind(node As SyntaxNode, kind As EnvDTE80.vsCMParameterKind) As SyntaxNode
            Dim parameter = TryCast(node, ParameterSyntax)

            If parameter Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            If Not IsValidParameterKind(kind) Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            Dim newModifierList = New List(Of SyntaxToken)

            ' TODO (tomescht): The Dev11 code allowed different sets of modifiers to be
            ' set when in batch mode vs non-batch mode.

            If (kind And EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional) <> 0 Then
                newModifierList.Add(SyntaxFactory.Token(SyntaxKind.OptionalKeyword))
            End If

            If (kind And EnvDTE80.vsCMParameterKind.vsCMParameterKindRef) <> 0 Then
                newModifierList.Add(SyntaxFactory.Token(SyntaxKind.ByRefKeyword))
            End If

            If (kind And EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray) <> 0 Then
                newModifierList.Add(SyntaxFactory.Token(SyntaxKind.ParamArrayKeyword))
            End If

            Return parameter.WithModifiers(SyntaxFactory.TokenList(newModifierList))
        End Function

        Private Function IsValidParameterKind(kind As EnvDTE80.vsCMParameterKind) As Boolean
            Select Case kind
                Case EnvDTE80.vsCMParameterKind.vsCMParameterKindNone,
                     EnvDTE80.vsCMParameterKind.vsCMParameterKindIn,
                     EnvDTE80.vsCMParameterKind.vsCMParameterKindRef,
                     EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional,
                     EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindIn,
                     EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional Or EnvDTE80.vsCMParameterKind.vsCMParameterKindRef,
                     EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray,
                     EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray Or EnvDTE80.vsCMParameterKind.vsCMParameterKindIn
                    Return True
            End Select

            Return False
        End Function

        Public Overrides Function ValidateFunctionKind(containerNode As SyntaxNode, kind As EnvDTE.vsCMFunction, name As String) As EnvDTE.vsCMFunction
            If kind = EnvDTE.vsCMFunction.vsCMFunctionSub Then
                Return If(name = "New" AndAlso Not TypeOf containerNode Is InterfaceBlockSyntax,
                          EnvDTE.vsCMFunction.vsCMFunctionConstructor,
                          kind)
            End If

            If kind = EnvDTE.vsCMFunction.vsCMFunctionFunction Then
                Return kind
            End If

            Throw Exceptions.ThrowEInvalidArg()
        End Function

        Public Overrides ReadOnly Property SupportsEventThrower As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Function GetCanOverride(memberNode As SyntaxNode) As Boolean
            Debug.Assert(TypeOf memberNode Is StatementSyntax)

            Dim member = TryCast(memberNode, StatementSyntax)
            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            If member.Parent.Kind = SyntaxKind.InterfaceBlock Then
                Return True
            End If

            Dim flags = member.GetModifierFlags()

            If (flags And ModifierFlags.NotOverridable) <> 0 Then
                Return False
            End If

            If (flags And ModifierFlags.MustOverride) <> 0 Then
                Return True
            End If

            If (flags And ModifierFlags.Overridable) <> 0 Then
                Return True
            End If

            If (flags And ModifierFlags.Overrides) <> 0 Then
                Return True
            End If

            Return False
        End Function

        Public Overrides Function SetCanOverride(memberNode As SyntaxNode, value As Boolean) As SyntaxNode
            Dim overrideKind = If(value, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)

            Return SetOverrideKind(memberNode, overrideKind)
        End Function

        Public Overrides Function GetClassKind(typeNode As SyntaxNode, typeSymbol As INamedTypeSymbol) As EnvDTE80.vsCMClassKind
            Debug.Assert(TypeOf typeNode Is ClassBlockSyntax OrElse
                         TypeOf typeNode Is ModuleBlockSyntax)

            Dim result As EnvDTE80.vsCMClassKind = 0

            Dim typeBlock = DirectCast(typeNode, TypeBlockSyntax)
            If TypeOf typeBlock Is ModuleBlockSyntax Then
                Return EnvDTE80.vsCMClassKind.vsCMClassKindModule
            End If

            Dim flags = typeBlock.GetModifierFlags()

            If (flags And ModifierFlags.Partial) <> 0 Then
                Return EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass
            End If

            If typeSymbol.DeclaringSyntaxReferences.Length > 1 Then
                Return EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass
            End If

            Return EnvDTE80.vsCMClassKind.vsCMClassKindMainClass
        End Function

        Private Function IsValidClassKind(kind As EnvDTE80.vsCMClassKind) As Boolean
            Return kind = EnvDTE80.vsCMClassKind.vsCMClassKindMainClass OrElse
                   kind = EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass
        End Function

        Public Overrides Function SetClassKind(typeNode As SyntaxNode, kind As EnvDTE80.vsCMClassKind) As SyntaxNode
            Debug.Assert(TypeOf typeNode Is ClassBlockSyntax OrElse
                         TypeOf typeNode Is ModuleBlockSyntax)

            Dim typeBlock = DirectCast(typeNode, StatementSyntax)
            If TypeOf typeBlock Is ModuleBlockSyntax Then
                If kind = EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone Then
                    Return typeBlock
                End If

                Throw Exceptions.ThrowENotImpl()
            End If

            If Not IsValidClassKind(kind) Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            Dim flags = typeBlock.GetModifierFlags()
            flags = flags And Not ModifierFlags.Partial

            If kind = EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass Then
                flags = flags Or ModifierFlags.Partial
            End If

            Return typeBlock.UpdateModifiers(flags)
        End Function

        Private Shared Function CollectComments(triviaList As IList(Of SyntaxTrivia)) As IList(Of SyntaxTrivia)
            Dim commentList = New List(Of SyntaxTrivia)
            Dim firstCommentFound = False

            For i = triviaList.Count - 1 To 0 Step -1
                Dim trivia = triviaList(i)
                Dim nextTrivia = If(i > 0, triviaList(i - 1), Nothing)

                If trivia.Kind = SyntaxKind.CommentTrivia Then
                    firstCommentFound = True
                    commentList.Add(trivia)
                ElseIf Not firstCommentFound AndAlso trivia.IsWhitespace() Then
                    Continue For
                ElseIf firstCommentFound AndAlso trivia.Kind = SyntaxKind.EndOfLineTrivia AndAlso nextTrivia.Kind = SyntaxKind.CommentTrivia Then
                    Continue For
                Else
                    Exit For
                End If
            Next

            commentList.Reverse()

            Return commentList
        End Function

        Public Overrides Function GetComment(node As SyntaxNode) As String
            Debug.Assert(TypeOf node Is StatementSyntax)

            Dim member = DirectCast(node, StatementSyntax)

            Dim firstToken = member.GetFirstToken()
            Dim triviaList = firstToken.LeadingTrivia
            Dim commentList = CollectComments(firstToken.LeadingTrivia.ToArray())

            If commentList.Count = 0 Then
                Return String.Empty
            End If

            Dim textBuilder = New StringBuilder()
            For Each trivia In commentList
                Debug.Assert(trivia.ToString().StartsWith("'", StringComparison.Ordinal))
                Dim commentText = trivia.ToString().Substring(1)

                textBuilder.AppendLine(commentText)
            Next

            Return textBuilder.ToString().TrimEnd()
        End Function

        Public Overrides Function SetComment(node As SyntaxNode, value As String) As SyntaxNode
            Debug.Assert(TypeOf node Is StatementSyntax)

            Dim member = DirectCast(node, StatementSyntax)
            Dim text = member.SyntaxTree.GetText(CancellationToken.None)
            Dim newLine = GetNewLineCharacter(text)

            Dim commentText = String.Empty

            If value IsNot Nothing Then
                Dim builder = New StringBuilder()

                For Each line In value.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                    builder.Append("' ")
                    builder.Append(line)
                    builder.Append(newLine)
                Next

                commentText = builder.ToString()
            End If

            Dim newTriviaList = SyntaxFactory.ParseLeadingTrivia(commentText)
            Dim leadingTriviaList = member.GetLeadingTrivia().ToList()

            Dim commentList = CollectComments(leadingTriviaList)
            If commentList.Count > 0 Then
                ' In this case, we're going to replace the existing comment.
                Dim firstIndex = leadingTriviaList.FindIndex(Function(t) t = commentList(0))
                Dim lastIndex = leadingTriviaList.FindIndex(Function(t) t = commentList(commentList.Count - 1))
                Dim count = lastIndex - firstIndex + 1

                leadingTriviaList.RemoveRange(firstIndex, count)

                ' Note: single line comments have a trailing new-line but that won't be
                ' returned by CollectComments. So, we may need to remove an additional new line below.
                If firstIndex < leadingTriviaList.Count AndAlso
                   leadingTriviaList(firstIndex).Kind = SyntaxKind.EndOfLineTrivia Then

                    leadingTriviaList.RemoveAt(firstIndex)
                End If

                For Each triviaElement In newTriviaList.Reverse()
                    leadingTriviaList.Insert(firstIndex, triviaElement)
                Next
            Else
                ' Otherwise, just add the comment to the end of the leading trivia.
                leadingTriviaList.AddRange(newTriviaList)
            End If

            Return member.WithLeadingTrivia(leadingTriviaList)
        End Function

        Public Overrides Function GetConstKind(variableNode As SyntaxNode) As EnvDTE80.vsCMConstKind
            If TypeOf variableNode Is EnumMemberDeclarationSyntax Then
                Return EnvDTE80.vsCMConstKind.vsCMConstKindConst
            End If

            Dim member = TryCast(GetNodeWithModifiers(variableNode), StatementSyntax)
            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim flags = member.GetModifierFlags()

            If (flags And ModifierFlags.Const) <> 0 Then
                Return EnvDTE80.vsCMConstKind.vsCMConstKindConst
            End If

            If (flags And ModifierFlags.ReadOnly) <> 0 Then
                Return EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly
            End If

            Return EnvDTE80.vsCMConstKind.vsCMConstKindNone
        End Function

        Private Function IsValidConstKind(kind As EnvDTE80.vsCMConstKind) As Boolean
            Return kind = EnvDTE80.vsCMConstKind.vsCMConstKindConst OrElse
                   kind = EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly OrElse
                   kind = EnvDTE80.vsCMConstKind.vsCMConstKindNone
        End Function

        Public Overrides Function SetConstKind(variableNode As SyntaxNode, kind As EnvDTE80.vsCMConstKind) As SyntaxNode
            If TypeOf variableNode Is EnumMemberDeclarationSyntax Then
                Throw Exceptions.ThrowENotImpl()
            End If

            If Not IsValidConstKind(kind) Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            Dim member = TryCast(GetNodeWithModifiers(variableNode), StatementSyntax)
            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim flags = member.GetModifierFlags()
            flags = flags And Not (ModifierFlags.Const Or ModifierFlags.ReadOnly Or ModifierFlags.Dim)

            If kind = EnvDTE80.vsCMConstKind.vsCMConstKindConst Then
                flags = flags Or ModifierFlags.Const
            ElseIf kind = EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly Then
                flags = flags Or ModifierFlags.ReadOnly
            End If

            If flags = 0 Then
                flags = flags Or ModifierFlags.Dim
            End If

            Return member.UpdateModifiers(flags)
        End Function

        Public Overrides Function GetDataTypeKind(typeNode As SyntaxNode, symbol As INamedTypeSymbol) As EnvDTE80.vsCMDataTypeKind
            Debug.Assert(TypeOf typeNode Is ClassBlockSyntax OrElse
                         TypeOf typeNode Is InterfaceBlockSyntax OrElse
                         TypeOf typeNode Is ModuleBlockSyntax OrElse
                         TypeOf typeNode Is StructureBlockSyntax)

            Dim typeBlock = DirectCast(typeNode, TypeBlockSyntax)
            If TypeOf typeBlock Is ModuleBlockSyntax Then
                Return EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindModule
            End If

            Dim flags = typeBlock.GetModifierFlags()
            If (flags And ModifierFlags.Partial) <> 0 Then
                Return EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial
            End If

            If symbol.DeclaringSyntaxReferences.Length > 1 Then
                Return EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial
            End If

            Return EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain
        End Function

        Private Function IsValidDataTypeKind(kind As EnvDTE80.vsCMDataTypeKind, allowModule As Boolean) As Boolean
            Return kind = EnvDTE80.vsCMClassKind.vsCMClassKindMainClass OrElse
                   kind = EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass OrElse
                   (allowModule AndAlso kind = EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindModule)
        End Function

        Public Overrides Function SetDataTypeKind(typeNode As SyntaxNode, kind As EnvDTE80.vsCMDataTypeKind) As SyntaxNode
            Debug.Assert(TypeOf typeNode Is ClassBlockSyntax OrElse
                         TypeOf typeNode Is InterfaceBlockSyntax OrElse
                         TypeOf typeNode Is ModuleBlockSyntax OrElse
                         TypeOf typeNode Is StructureBlockSyntax)

            Dim typeBlock = DirectCast(typeNode, TypeBlockSyntax)
            If TypeOf typeBlock Is InterfaceBlockSyntax Then
                Throw Exceptions.ThrowENotImpl()
            End If

            Dim allowModule = TypeOf typeBlock Is ClassBlockSyntax OrElse
                              TypeOf typeBlock Is ModuleBlockSyntax

            If Not IsValidDataTypeKind(kind, allowModule) Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim flags = typeBlock.GetModifierFlags()
            flags = flags And Not ModifierFlags.Partial

            If kind = EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial Then
                flags = flags Or ModifierFlags.Partial
            End If

            ' VB supports changing a Module to a Class and vice versa.
            If TypeOf typeBlock Is ModuleBlockSyntax Then
                If kind = EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain OrElse
                   kind = EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial Then

                    Dim moduleBlock = DirectCast(typeBlock, ModuleBlockSyntax)

                    typeBlock = SyntaxFactory.ClassBlock(
                        classStatement:=SyntaxFactory.ClassStatement(
                            attributeLists:=moduleBlock.ModuleStatement.AttributeLists,
                            modifiers:=moduleBlock.ModuleStatement.Modifiers,
                            classKeyword:=SyntaxFactory.Token(moduleBlock.ModuleStatement.ModuleKeyword.LeadingTrivia, SyntaxKind.ClassKeyword, moduleBlock.ModuleStatement.ModuleKeyword.TrailingTrivia),
                            identifier:=moduleBlock.ModuleStatement.Identifier,
                            typeParameterList:=moduleBlock.ModuleStatement.TypeParameterList),
                        [inherits]:=moduleBlock.Inherits,
                        [implements]:=moduleBlock.Implements,
                        members:=moduleBlock.Members,
                        endClassStatement:=SyntaxFactory.EndClassStatement(
                            endKeyword:=moduleBlock.EndModuleStatement.EndKeyword,
                            blockKeyword:=SyntaxFactory.Token(moduleBlock.EndModuleStatement.BlockKeyword.LeadingTrivia, SyntaxKind.ClassKeyword, moduleBlock.EndModuleStatement.BlockKeyword.TrailingTrivia)))
                End If
            ElseIf TypeOf typeBlock Is ClassBlockSyntax Then
                If kind = EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindModule Then

                    flags = flags And Not ModifierFlags.Shared
                    Dim classBlock = DirectCast(typeBlock, ClassBlockSyntax)

                    typeBlock = SyntaxFactory.ModuleBlock(
                        moduleStatement:=SyntaxFactory.ModuleStatement(
                            attributeLists:=classBlock.ClassStatement.AttributeLists,
                            modifiers:=classBlock.ClassStatement.Modifiers,
                            moduleKeyword:=SyntaxFactory.Token(classBlock.ClassStatement.ClassKeyword.LeadingTrivia, SyntaxKind.ModuleKeyword, classBlock.ClassStatement.ClassKeyword.TrailingTrivia),
                            identifier:=classBlock.ClassStatement.Identifier,
                            typeParameterList:=classBlock.ClassStatement.TypeParameterList),
                        [inherits]:=Nothing,
                        [implements]:=Nothing,
                        members:=classBlock.Members,
                        endModuleStatement:=SyntaxFactory.EndModuleStatement(
                            endKeyword:=classBlock.EndClassStatement.EndKeyword,
                            blockKeyword:=SyntaxFactory.Token(classBlock.EndClassStatement.BlockKeyword.LeadingTrivia, SyntaxKind.ModuleKeyword, classBlock.EndClassStatement.BlockKeyword.TrailingTrivia)))
                End If
            End If

            Return typeBlock.UpdateModifiers(flags)
        End Function

        Private Shared Function GetDocCommentNode(memberDeclaration As StatementSyntax) As DocumentationCommentTriviaSyntax
            Dim docCommentTrivia = memberDeclaration _
                .GetLeadingTrivia() _
                .Reverse() _
                .FirstOrDefault(Function(t) t.Kind = SyntaxKind.DocumentationCommentTrivia)

            If docCommentTrivia.Kind <> SyntaxKind.DocumentationCommentTrivia Then
                Return Nothing
            End If

            Return DirectCast(docCommentTrivia.GetStructure(), DocumentationCommentTriviaSyntax)
        End Function

        Public Overrides Function GetDocComment(node As SyntaxNode) As String
            Debug.Assert(TypeOf node Is StatementSyntax)

            Dim member = DirectCast(node, StatementSyntax)
            Dim documentationComment = GetDocCommentNode(member)
            If documentationComment Is Nothing Then
                Return String.Empty
            End If

            Dim text = member.SyntaxTree.GetText(CancellationToken.None)
            Dim newLine = GetNewLineCharacter(text)

            Dim lines = documentationComment.ToString().Split({newLine}, StringSplitOptions.None)

            ' trim off leading whitespace and exterior trivia.
            Dim lengthToStrip = lines(0).GetLeadingWhitespace().Length
            Dim linesCount = lines.Length

            For i = 1 To lines.Length - 1
                Dim line = lines(i).TrimStart()
                If line.StartsWith("'''", StringComparison.Ordinal) Then
                    lines(i) = line.Substring(3)
                End If
            Next

            Return lines.Join(newLine).TrimEnd()
        End Function

        Public Overrides Function SetDocComment(node As SyntaxNode, value As String) As SyntaxNode
            Debug.Assert(TypeOf node Is StatementSyntax)

            Dim member = DirectCast(node, StatementSyntax)
            Dim triviaList = CType(Nothing, SyntaxTriviaList)

            If value IsNot Nothing Then
                Dim text = member.SyntaxTree.GetText(CancellationToken.None)
                Dim newLine = GetNewLineCharacter(text)
                Dim builder = New StringBuilder()

                For Each line In value.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                    builder.Append("''' ")
                    builder.Append(line)
                    builder.Append(newLine)
                Next

                triviaList = SyntaxFactory.ParseLeadingTrivia(builder.ToString())
            End If

            Dim leadingTriviaList = member.GetLeadingTrivia().ToList()
            Dim documentationComment = GetDocCommentNode(member)

            If documentationComment IsNot Nothing Then
                ' In this case, we're going to replace the existing XML doc comment.
                Dim index = leadingTriviaList.FindIndex(Function(t) t = documentationComment.ParentTrivia)
                leadingTriviaList.RemoveAt(index)

                For Each triviaElement In triviaList.Reverse()
                    leadingTriviaList.Insert(index, triviaElement)
                Next
            Else
                ' Otherwise, just add the XML doc comment to the end of the leading trivia.
                leadingTriviaList.AddRange(triviaList)
            End If

            Return member.WithLeadingTrivia(leadingTriviaList)
        End Function

        Public Overrides Function GetFunctionKind(symbol As IMethodSymbol) As EnvDTE.vsCMFunction
            If symbol.IsOverride AndAlso symbol.Name = "Finalize" Then
                Return EnvDTE.vsCMFunction.vsCMFunctionDestructor
            End If

            Select Case symbol.MethodKind
                Case MethodKind.Ordinary,
                     MethodKind.DeclareMethod
                    Return If(symbol.ReturnsVoid, EnvDTE.vsCMFunction.vsCMFunctionSub, EnvDTE.vsCMFunction.vsCMFunctionFunction)

                Case MethodKind.Constructor,
                     MethodKind.StaticConstructor
                    Return EnvDTE.vsCMFunction.vsCMFunctionConstructor

                Case MethodKind.UserDefinedOperator
                    Return EnvDTE.vsCMFunction.vsCMFunctionOperator

                Case MethodKind.PropertyGet
                    Return EnvDTE.vsCMFunction.vsCMFunctionPropertyGet
                Case MethodKind.PropertySet
                    Return EnvDTE.vsCMFunction.vsCMFunctionPropertySet

                Case MethodKind.EventAdd
                    Return CType(EnvDTE80.vsCMFunction2.vsCMFunctionAddHandler, EnvDTE.vsCMFunction)
                Case MethodKind.EventRemove
                    Return CType(EnvDTE80.vsCMFunction2.vsCMFunctionRemoveHandler, EnvDTE.vsCMFunction)
                Case MethodKind.EventRaise
                    Return CType(EnvDTE80.vsCMFunction2.vsCMFunctionRaiseEvent, EnvDTE.vsCMFunction)
            End Select

            Throw Exceptions.ThrowEUnexpected()
        End Function

        Public Overrides Function GetInheritanceKind(typeNode As SyntaxNode, typeSymbol As INamedTypeSymbol) As EnvDTE80.vsCMInheritanceKind
            Dim result As EnvDTE80.vsCMInheritanceKind = 0

            If typeSymbol.IsSealed Then
                result = result Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed
            ElseIf typeSymbol.IsAbstract Then
                result = result Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract
            End If

            ' Old VB code model had a special case to check other parts for Shadows, so we'll do that here..
            Dim statements = typeSymbol.DeclaringSyntaxReferences _
                .Select(Function(r) TryCast(r.GetSyntax(), StatementSyntax)) _
                .Where(Function(s) s IsNot Nothing)

            For Each statement In statements
                Dim modifiers = SyntaxFactory.TokenList(statement.GetModifiers())

                If modifiers.Any(SyntaxKind.ShadowsKeyword) Then
                    result = result Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew
                    Exit For
                End If
            Next

            Return result
        End Function

        Private Function IsValidInheritanceKind(kind As EnvDTE80.vsCMInheritanceKind) As Boolean
            Return kind = EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract OrElse
                   kind = EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone OrElse
                   kind = EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed OrElse
                   kind = EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew OrElse
                   kind = (EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract) OrElse
                   kind = (EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed)
        End Function

        Public Overrides Function SetInheritanceKind(typeNode As SyntaxNode, kind As EnvDTE80.vsCMInheritanceKind) As SyntaxNode
            Debug.Assert(TypeOf typeNode Is ClassBlockSyntax OrElse
                         TypeOf typeNode Is ModuleBlockSyntax)

            If TypeOf typeNode Is ModuleBlockSyntax Then
                If kind = EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone Then
                    Return typeNode
                End If

                Throw Exceptions.ThrowENotImpl()
            End If

            If Not IsValidInheritanceKind(kind) Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            Dim member = TryCast(typeNode, StatementSyntax)
            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim flags = member.GetModifierFlags()
            flags = flags And Not (ModifierFlags.MustInherit Or ModifierFlags.NotInheritable Or ModifierFlags.Shadows)

            If kind <> EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone Then
                If (kind And EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract) <> 0 Then
                    flags = flags Or ModifierFlags.MustInherit
                ElseIf (kind And EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed) <> 0 Then
                    flags = flags Or ModifierFlags.NotInheritable
                End If

                If (kind And EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew) <> 0 Then
                    flags = flags Or ModifierFlags.Shadows
                End If
            End If

            Return member.UpdateModifiers(flags)
        End Function

        Public Overrides Function GetMustImplement(memberNode As SyntaxNode) As Boolean
            Debug.Assert(TypeOf memberNode Is StatementSyntax)

            Dim member = TryCast(memberNode, StatementSyntax)
            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            If member.Parent.Kind = SyntaxKind.InterfaceBlock Then
                Return True
            End If

            Dim flags = member.GetModifierFlags()

            Return (flags And ModifierFlags.MustOverride) <> 0
        End Function

        Public Overrides Function SetMustImplement(memberNode As SyntaxNode, value As Boolean) As SyntaxNode
            Dim overrideKind = If(value, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)

            Return SetOverrideKind(memberNode, overrideKind)
        End Function

        Public Overrides Function GetOverrideKind(memberNode As SyntaxNode) As EnvDTE80.vsCMOverrideKind
            Debug.Assert(TypeOf memberNode Is DeclarationStatementSyntax)

            Dim member = TryCast(memberNode, DeclarationStatementSyntax)
            If member IsNot Nothing Then
                Dim modifiers = SyntaxFactory.TokenList(member.GetModifiers())
                Dim result As EnvDTE80.vsCMOverrideKind = 0

                If modifiers.Any(SyntaxKind.ShadowsKeyword) Then
                    result = result Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew
                End If

                If modifiers.Any(SyntaxKind.OverridesKeyword) Then
                    result = result Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride
                End If

                If modifiers.Any(SyntaxKind.MustOverrideKeyword) Or member.IsParentKind(SyntaxKind.InterfaceBlock) Then
                    result = result Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract
                End If

                If modifiers.Any(SyntaxKind.OverridableKeyword) Then
                    result = result Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual
                ElseIf modifiers.Any(SyntaxKind.NotOverridableKeyword) Then
                    result = result Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed
                End If

                Return result
            End If

            Throw New InvalidOperationException
        End Function

        Private Function IsValidOverrideKind(kind As EnvDTE80.vsCMOverrideKind) As Boolean
            Return kind = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed OrElse
                   kind = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew OrElse
                   kind = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride OrElse
                   kind = (EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed) OrElse
                   kind = (EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual Or EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew) OrElse
                   kind = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract OrElse
                   kind = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual OrElse
                   kind = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone
        End Function

        Public Overrides Function SetOverrideKind(memberNode As SyntaxNode, kind As EnvDTE80.vsCMOverrideKind) As SyntaxNode
            Dim member = TryCast(memberNode, StatementSyntax)
            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            If TypeOf memberNode.Parent Is ModuleBlockSyntax OrElse
               TypeOf memberNode.Parent Is InterfaceBlockSyntax OrElse
               TypeOf memberNode.Parent Is PropertyBlockSyntax OrElse
               TypeOf memberNode.Parent Is PropertyStatementSyntax Then

                Throw Exceptions.ThrowENotImpl()
            End If

            If Not IsValidOverrideKind(kind) Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            Dim flags = member.GetModifierFlags()
            flags = flags And Not (ModifierFlags.NotOverridable Or ModifierFlags.Shadows Or ModifierFlags.Overrides Or ModifierFlags.MustOverride Or ModifierFlags.Overridable)

            Select Case kind
                Case EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed
                    flags = flags Or ModifierFlags.NotOverridable
                Case EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew
                    flags = flags Or ModifierFlags.Shadows
                Case EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride
                    flags = flags Or ModifierFlags.Overrides
                Case EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract
                    flags = flags Or ModifierFlags.MustOverride
                Case EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual
                    flags = flags Or ModifierFlags.Overridable
                Case EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed
                    flags = flags Or ModifierFlags.NotOverridable Or ModifierFlags.Overrides
                Case EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew
                    flags = flags Or ModifierFlags.Overridable Or ModifierFlags.Shadows
            End Select

            Dim resultMember = member.UpdateModifiers(flags)

            If (flags And ModifierFlags.MustOverride) <> 0 Then
                If TypeOf resultMember Is MethodBlockBaseSyntax Then
                    resultMember = DirectCast(resultMember, MethodBlockBaseSyntax).BlockStatement
                ElseIf TypeOf resultMember Is PropertyBlockSyntax Then
                    resultMember = DirectCast(resultMember, PropertyBlockSyntax).PropertyStatement
                End If
            Else
                If TypeOf resultMember Is MethodStatementSyntax Then
                    If resultMember.Kind = SyntaxKind.FunctionStatement Then
                        resultMember = SyntaxFactory.FunctionBlock(
                            subOrFunctionStatement:=DirectCast(resultMember, MethodStatementSyntax),
                            statements:=Nothing,
                            endSubOrFunctionStatement:=SyntaxFactory.EndFunctionStatement())
                    ElseIf resultMember.Kind = SyntaxKind.SubStatement Then
                        resultMember = SyntaxFactory.SubBlock(
                            subOrFunctionStatement:=DirectCast(resultMember, MethodStatementSyntax),
                            statements:=Nothing,
                            endSubOrFunctionStatement:=SyntaxFactory.EndSubStatement())
                    End If
                ElseIf TypeOf resultMember Is PropertyStatementSyntax Then
                    Dim propertyStatement = DirectCast(resultMember, PropertyStatementSyntax)

                    Dim parameterName = "value"
                    If propertyStatement.Identifier.GetTypeCharacter() <> TypeCharacter.None Then
                        parameterName &= propertyStatement.Identifier.GetTypeCharacter().GetTypeCharacterString()
                    End If

                    Dim returnType = propertyStatement.GetReturnType()
                    Dim asClauseText = If(returnType IsNot Nothing,
                                          " As " & returnType.ToString(),
                                          String.Empty)

                    resultMember = SyntaxFactory.PropertyBlock(
                        propertyStatement:=propertyStatement,
                        accessors:=SyntaxFactory.List(Of AccessorBlockSyntax)({
                            SyntaxFactory.GetAccessorBlock(
                                accessorStatement:=SyntaxFactory.GetAccessorStatement(),
                                statements:=Nothing,
                                endAccessorStatement:=SyntaxFactory.EndGetStatement()),
                            SyntaxFactory.SetAccessorBlock(
                                accessorStatement:=SyntaxFactory.SetAccessorStatement(
                                    attributeLists:=Nothing,
                                    modifiers:=Nothing,
                                    parameterList:=SyntaxFactory.ParseParameterList("(" & parameterName & asClauseText & ")")),
                                statements:=Nothing,
                                endAccessorStatement:=SyntaxFactory.EndSetStatement())
                            }))
                End If
            End If

            Return resultMember
        End Function

        Public Overrides Function GetIsAbstract(memberNode As SyntaxNode, symbol As ISymbol) As Boolean
            Return symbol.IsAbstract
        End Function

        Public Overrides Function SetIsAbstract(memberNode As SyntaxNode, value As Boolean) As SyntaxNode
            If TypeOf memberNode Is StructureBlockSyntax OrElse
               TypeOf memberNode Is ModuleBlockSyntax Then
                Throw Exceptions.ThrowENotImpl()
            End If

            Dim member = TryCast(memberNode, StatementSyntax)
            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim flags = member.GetModifierFlags()

            If value Then
                If TypeOf memberNode Is TypeBlockSyntax Then
                    flags = flags Or ModifierFlags.MustInherit
                Else
                    flags = flags Or ModifierFlags.MustOverride
                End If
            Else
                If TypeOf memberNode Is TypeBlockSyntax Then
                    flags = flags And Not ModifierFlags.MustInherit
                Else
                    flags = flags And Not ModifierFlags.MustOverride
                End If
            End If

            Return member.UpdateModifiers(flags)
        End Function

        Public Overrides Function GetIsConstant(variableNode As SyntaxNode) As Boolean
            If TypeOf variableNode Is EnumMemberDeclarationSyntax Then
                Return True
            End If

            Dim member = TryCast(GetNodeWithModifiers(variableNode), StatementSyntax)
            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim flags = member.GetModifierFlags()

            ' VB legacy code model returns True for both Const and ReadOnly fields
            Return (flags And (ModifierFlags.Const Or ModifierFlags.ReadOnly)) <> 0
        End Function

        Public Overrides Function SetIsConstant(variableNode As SyntaxNode, value As Boolean) As SyntaxNode
            Dim constKind = If(value, EnvDTE80.vsCMConstKind.vsCMConstKindConst, EnvDTE80.vsCMConstKind.vsCMConstKindNone)

            Return SetConstKind(variableNode, constKind)
        End Function

        Public Overrides Function GetIsDefault(propertyNode As SyntaxNode) As Boolean
            Debug.Assert(TypeOf propertyNode Is PropertyBlockSyntax OrElse
                         TypeOf propertyNode Is PropertyStatementSyntax)

            If TypeOf propertyNode Is PropertyStatementSyntax Then
                Return False
            End If

            Dim propertyBlock = TryCast(propertyNode, PropertyBlockSyntax)
            If propertyBlock Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Return propertyBlock.PropertyStatement.Modifiers.Any(SyntaxKind.DefaultKeyword)
        End Function

        Public Overrides Function SetIsDefault(propertyNode As SyntaxNode, value As Boolean) As SyntaxNode
            Debug.Assert(TypeOf propertyNode Is PropertyBlockSyntax OrElse
                         TypeOf propertyNode Is PropertyStatementSyntax)

            Dim member = DirectCast(propertyNode, StatementSyntax)

            Dim flags = member.GetModifierFlags()
            flags = flags And Not ModifierFlags.Default

            If value Then
                flags = flags Or ModifierFlags.Default
            End If

            Return member.UpdateModifiers(flags)
        End Function

        Public Overrides Function GetIsGeneric(node As SyntaxNode) As Boolean
            Debug.Assert(TypeOf node Is StatementSyntax)

            Return DirectCast(node, StatementSyntax).GetArity() > 0
        End Function

        Public Overrides Function GetIsPropertyStyleEvent(eventNode As SyntaxNode) As Boolean
            Debug.Assert(TypeOf eventNode Is EventStatementSyntax OrElse
                         TypeOf eventNode Is EventBlockSyntax)

            Return TypeOf eventNode Is EventBlockSyntax
        End Function

        Public Overrides Function GetIsShared(memberNode As SyntaxNode, symbol As ISymbol) As Boolean
            Return _
                (symbol.Kind = SymbolKind.NamedType AndAlso DirectCast(symbol, INamedTypeSymbol).TypeKind = TypeKind.Module) OrElse
                symbol.IsStatic
        End Function

        Public Overrides Function SetIsShared(memberNode As SyntaxNode, value As Boolean) As SyntaxNode
            If TypeOf memberNode Is TypeBlockSyntax Then
                Throw Exceptions.ThrowENotImpl()
            End If

            Dim member = TryCast(memberNode, StatementSyntax)
            If member Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim parentType = TryCast(member.Parent, DeclarationStatementSyntax)
            If parentType Is Nothing Then
                Throw Exceptions.ThrowEFail()
            End If

            If TypeOf parentType Is ModuleBlockSyntax OrElse
               TypeOf parentType Is InterfaceBlockSyntax OrElse
               TypeOf parentType Is EnumBlockSyntax Then
                Throw Exceptions.ThrowENotImpl()
            End If

            Dim flags = member.GetModifierFlags() And Not ModifierFlags.Dim

            If value Then
                flags = flags Or ModifierFlags.Shared
            Else
                flags = flags And Not ModifierFlags.Shared
            End If

            If flags = 0 AndAlso member.IsKind(SyntaxKind.FieldDeclaration) Then
                flags = flags Or ModifierFlags.Dim
            End If

            Return member.UpdateModifiers(flags)
        End Function

        Public Overrides Function GetReadWrite(memberNode As SyntaxNode) As EnvDTE80.vsCMPropertyKind
            Debug.Assert(TypeOf memberNode Is PropertyBlockSyntax OrElse
                         TypeOf memberNode Is PropertyStatementSyntax)

            Dim propertyStatement = TryCast(memberNode, PropertyStatementSyntax)

            If propertyStatement Is Nothing Then
                Dim propertyBlock = TryCast(memberNode, PropertyBlockSyntax)
                If propertyBlock IsNot Nothing Then
                    propertyStatement = propertyBlock.PropertyStatement
                End If
            End If

            If propertyStatement IsNot Nothing Then
                If propertyStatement.Modifiers.Any(SyntaxKind.WriteOnlyKeyword) Then
                    Return EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly
                ElseIf propertyStatement.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) Then
                    Return EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly
                Else
                    Return EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite
                End If
            End If

            Throw Exceptions.ThrowEUnexpected()
        End Function

        Private Function SetDelegateType(delegateStatement As DelegateStatementSyntax, typeSymbol As ITypeSymbol) As DelegateStatementSyntax
            ' Remove the leading and trailing trivia and save it for reattachment later.
            Dim leadingTrivia = delegateStatement.GetLeadingTrivia()
            Dim trailingTrivia = delegateStatement.GetTrailingTrivia()
            delegateStatement = delegateStatement _
                .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                .WithTrailingTrivia(SyntaxTriviaList.Empty)

            If typeSymbol Is Nothing Then
                ' If no type is specified (e.g. CodeElement.Type = Nothing), we just convert to a Sub
                ' if it isn't one already.
                If delegateStatement.IsKind(SyntaxKind.DelegateFunctionStatement) Then
                    delegateStatement = SyntaxFactory.DelegateSubStatement(
                        attributeLists:=delegateStatement.AttributeLists,
                        modifiers:=delegateStatement.Modifiers,
                        identifier:=delegateStatement.Identifier,
                        typeParameterList:=delegateStatement.TypeParameterList,
                        parameterList:=delegateStatement.ParameterList,
                        asClause:=Nothing)
                End If
            Else
                Dim typeName = typeSymbol.ToDisplayString(s_setTypeFormat)
                Dim newType = SyntaxFactory.ParseTypeName(typeName)

                ' If this is a Sub, convert to a Function
                If delegateStatement.IsKind(SyntaxKind.DelegateSubStatement) Then
                    delegateStatement = SyntaxFactory.DelegateFunctionStatement(
                        attributeLists:=delegateStatement.AttributeLists,
                        modifiers:=delegateStatement.Modifiers,
                        identifier:=delegateStatement.Identifier,
                        typeParameterList:=delegateStatement.TypeParameterList,
                        parameterList:=delegateStatement.ParameterList,
                        asClause:=delegateStatement.AsClause)
                End If

                If delegateStatement.AsClause IsNot Nothing Then
                    Debug.Assert(TypeOf delegateStatement.AsClause Is SimpleAsClauseSyntax)

                    Dim oldType = DirectCast(delegateStatement.AsClause, SimpleAsClauseSyntax).Type
                    delegateStatement = delegateStatement.ReplaceNode(oldType, newType)
                Else
                    delegateStatement = delegateStatement.WithAsClause(SyntaxFactory.SimpleAsClause(newType))
                End If
            End If

            Return delegateStatement _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia)
        End Function

        Private Function SetEventType(eventStatement As EventStatementSyntax, typeSymbol As ITypeSymbol) As EventStatementSyntax
            If typeSymbol Is Nothing Then
                Throw Exceptions.ThrowEInvalidArg()
            End If
            ' Remove the leading and trailing trivia and save it for reattachment later.
            Dim leadingTrivia = eventStatement.GetLeadingTrivia()
            Dim trailingTrivia = eventStatement.GetTrailingTrivia()
            eventStatement = eventStatement _
                .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                .WithTrailingTrivia(SyntaxTriviaList.Empty)

            Dim typeName = typeSymbol.ToDisplayString(s_setTypeFormat)
            Dim newType = SyntaxFactory.ParseTypeName(typeName)

            ' If the event has a parameter list, we need to remove it.
            If eventStatement.ParameterList IsNot Nothing Then
                eventStatement = eventStatement.WithParameterList(Nothing)
            End If

            If eventStatement.AsClause IsNot Nothing Then
                Debug.Assert(TypeOf eventStatement.AsClause Is SimpleAsClauseSyntax)

                Dim oldType = DirectCast(eventStatement.AsClause, SimpleAsClauseSyntax).Type
                eventStatement = eventStatement.ReplaceNode(oldType, newType)
            Else
                eventStatement = eventStatement.WithAsClause(SyntaxFactory.SimpleAsClause(newType))
            End If

            Return eventStatement _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia)
        End Function

        Private Function SetEventType(eventBlock As EventBlockSyntax, typeSymbol As ITypeSymbol) As EventBlockSyntax
            If typeSymbol Is Nothing Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            Dim typeName = typeSymbol.ToDisplayString(s_setTypeFormat)
            Dim newType = SyntaxFactory.ParseTypeName(typeName)

            ' Update the event statement
            Dim eventStatement = eventBlock.EventStatement
            Dim leadingTrivia = eventStatement.GetLeadingTrivia()
            Dim trailingTrivia = eventStatement.GetTrailingTrivia()
            eventStatement = eventStatement _
                .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                .WithTrailingTrivia(SyntaxTriviaList.Empty)

            If eventStatement.AsClause IsNot Nothing Then
                Debug.Assert(TypeOf eventStatement.AsClause Is SimpleAsClauseSyntax)

                Dim oldType = DirectCast(eventStatement.AsClause, SimpleAsClauseSyntax).Type
                eventStatement = eventStatement.ReplaceNode(oldType, newType)
            Else
                eventStatement = eventStatement.WithAsClause(SyntaxFactory.SimpleAsClause(newType))
            End If

            eventStatement = eventStatement _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia)
            eventBlock = eventBlock.WithEventStatement(eventStatement)

            For i = 0 To eventBlock.Accessors.Count - 1
                Dim accessorBlock = eventBlock.Accessors(i)
                Dim newAccessorBlock = accessorBlock

                If accessorBlock.Kind = SyntaxKind.AddHandlerAccessorBlock OrElse
                   accessorBlock.Kind = SyntaxKind.RemoveHandlerAccessorBlock Then

                    ' Update the first parameter of the AddHandler or RemoveHandler statements
                    Dim firstParameter = accessorBlock.BlockStatement.ParameterList.Parameters.FirstOrDefault()
                    If firstParameter IsNot Nothing Then
                        Dim newFirstParameter As ParameterSyntax
                        If firstParameter.AsClause IsNot Nothing Then
                            Debug.Assert(TypeOf firstParameter.AsClause Is SimpleAsClauseSyntax)

                            Dim oldType = DirectCast(firstParameter.AsClause, SimpleAsClauseSyntax).Type
                            newFirstParameter = firstParameter.ReplaceNode(oldType, newType)
                        Else
                            newFirstParameter = firstParameter.WithAsClause(SyntaxFactory.SimpleAsClause(newType))
                        End If

                        newFirstParameter = newFirstParameter _
                            .WithLeadingTrivia(firstParameter.GetLeadingTrivia()) _
                            .WithTrailingTrivia(firstParameter.GetTrailingTrivia())

                        newAccessorBlock = accessorBlock.ReplaceNode(firstParameter, newFirstParameter)
                    End If
                ElseIf accessorBlock.Kind = SyntaxKind.RaiseEventAccessorBlock Then
                    ' For RaiseEvent, we replace the whole signature with the delegate's invoke method

                    Dim namedTypeSymbol = TryCast(typeSymbol, INamedTypeSymbol)
                    If namedTypeSymbol IsNot Nothing Then
                        Dim invokeMethod = namedTypeSymbol.DelegateInvokeMethod
                        If invokeMethod IsNot Nothing Then
                            Dim parameterStrings = invokeMethod.Parameters.Select(Function(p) p.ToDisplayString(s_raiseEventSignatureFormat))
                            Dim parameterListString = "("c & String.Join(", ", parameterStrings) & ")"c
                            Dim newParameterList = SyntaxFactory.ParseParameterList(parameterListString)

                            newParameterList = newParameterList.WithTrailingTrivia(accessorBlock.BlockStatement.ParameterList.GetTrailingTrivia())
                            newAccessorBlock = accessorBlock.ReplaceNode(accessorBlock.BlockStatement.ParameterList, newParameterList)
                        End If
                    End If
                End If

                If accessorBlock IsNot newAccessorBlock Then
                    eventBlock = eventBlock.ReplaceNode(accessorBlock, newAccessorBlock)
                End If
            Next

            Return eventBlock
        End Function

        Private Function SetMethodType(declareStatement As DeclareStatementSyntax, typeSymbol As ITypeSymbol) As DeclareStatementSyntax
            ' Remove the leading and trailing trivia and save it for reattachment later.
            Dim leadingTrivia = declareStatement.GetLeadingTrivia()
            Dim trailingTrivia = declareStatement.GetTrailingTrivia()
            declareStatement = declareStatement _
                .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                .WithTrailingTrivia(SyntaxTriviaList.Empty)

            If typeSymbol Is Nothing Then
                ' If no type is specified (e.g. CodeElement.Type = Nothing), we just convert to a Sub
                ' if it isn't one already.
                If declareStatement.IsKind(SyntaxKind.DeclareFunctionStatement) Then
                    declareStatement = SyntaxFactory.DeclareSubStatement(
                        attributeLists:=declareStatement.AttributeLists,
                        modifiers:=declareStatement.Modifiers,
                        declareKeyword:=declareStatement.DeclareKeyword,
                        charsetKeyword:=declareStatement.CharsetKeyword,
                        subOrFunctionKeyword:=SyntaxFactory.Token(SyntaxKind.SubKeyword),
                        identifier:=declareStatement.Identifier,
                        libKeyword:=declareStatement.LibKeyword,
                        libraryName:=declareStatement.LibraryName,
                        aliasKeyword:=declareStatement.AliasKeyword,
                        aliasName:=declareStatement.AliasName,
                        parameterList:=declareStatement.ParameterList,
                        asClause:=Nothing)
                End If
            Else
                Dim newType = SyntaxFactory.ParseTypeName(typeSymbol.ToDisplayString(s_setTypeFormat))

                declareStatement = SyntaxFactory.DeclareFunctionStatement(
                    attributeLists:=declareStatement.AttributeLists,
                    modifiers:=declareStatement.Modifiers,
                    declareKeyword:=declareStatement.DeclareKeyword,
                    charsetKeyword:=declareStatement.CharsetKeyword,
                    subOrFunctionKeyword:=SyntaxFactory.Token(SyntaxKind.FunctionKeyword),
                    identifier:=declareStatement.Identifier,
                    libKeyword:=declareStatement.LibKeyword,
                    libraryName:=declareStatement.LibraryName,
                    aliasKeyword:=declareStatement.AliasKeyword,
                    aliasName:=declareStatement.AliasName,
                    parameterList:=declareStatement.ParameterList,
                    asClause:=declareStatement.AsClause)

                If declareStatement.AsClause IsNot Nothing Then
                    Debug.Assert(TypeOf declareStatement.AsClause Is SimpleAsClauseSyntax)

                    Dim oldType = DirectCast(declareStatement.AsClause, SimpleAsClauseSyntax).Type
                    declareStatement = declareStatement.ReplaceNode(oldType, newType)
                Else
                    declareStatement = declareStatement.WithAsClause(SyntaxFactory.SimpleAsClause(newType))
                End If
            End If

            Return declareStatement.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia)
        End Function

        Private Function SetMethodType(methodStatement As MethodStatementSyntax, typeSymbol As ITypeSymbol) As MethodStatementSyntax
            ' Remove the leading and trailing trivia and save it for reattachment later.
            Dim leadingTrivia = methodStatement.GetLeadingTrivia()
            Dim trailingTrivia = methodStatement.GetTrailingTrivia()
            methodStatement = methodStatement _
                .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                .WithTrailingTrivia(SyntaxTriviaList.Empty)

            If typeSymbol Is Nothing Then
                ' If no type is specified (e.g. CodeElement.Type = Nothing), we just convert to a Sub
                ' if it isn't one already.
                If methodStatement.IsKind(SyntaxKind.FunctionStatement) Then
                    methodStatement = SyntaxFactory.SubStatement(
                        attributeLists:=methodStatement.AttributeLists,
                        modifiers:=methodStatement.Modifiers,
                        identifier:=methodStatement.Identifier,
                        typeParameterList:=methodStatement.TypeParameterList,
                        parameterList:=methodStatement.ParameterList,
                        asClause:=Nothing,
                        handlesClause:=methodStatement.HandlesClause,
                        implementsClause:=methodStatement.ImplementsClause)
                End If
            Else
                Dim typeName = typeSymbol.ToDisplayString(s_setTypeFormat)
                Dim newType = SyntaxFactory.ParseTypeName(typeName)

                ' If this is a Sub, convert to a Function
                If methodStatement.IsKind(SyntaxKind.SubStatement) Then
                    methodStatement = SyntaxFactory.FunctionStatement(
                        attributeLists:=methodStatement.AttributeLists,
                        modifiers:=methodStatement.Modifiers,
                        identifier:=methodStatement.Identifier,
                        typeParameterList:=methodStatement.TypeParameterList,
                        parameterList:=methodStatement.ParameterList,
                        asClause:=methodStatement.AsClause,
                        handlesClause:=methodStatement.HandlesClause,
                        implementsClause:=methodStatement.ImplementsClause)
                End If

                If methodStatement.AsClause IsNot Nothing Then
                    Debug.Assert(TypeOf methodStatement.AsClause Is SimpleAsClauseSyntax)

                    Dim oldType = DirectCast(methodStatement.AsClause, SimpleAsClauseSyntax).Type
                    methodStatement = methodStatement.ReplaceNode(oldType, newType)
                Else
                    methodStatement = methodStatement.WithAsClause(SyntaxFactory.SimpleAsClause(newType))
                End If
            End If

            Return methodStatement _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia)
        End Function

        Private Function SetMethodType(methodBlock As MethodBlockSyntax, typeSymbol As ITypeSymbol) As MethodBlockSyntax
            ' Remove the leading and trailing trivia and save it for reattachment later.
            Dim leadingTrivia = methodBlock.GetLeadingTrivia()
            Dim trailingTrivia = methodBlock.GetTrailingTrivia()
            methodBlock = methodBlock _
                .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                .WithTrailingTrivia(SyntaxTriviaList.Empty)

            Dim methodStatement = SetMethodType(DirectCast(methodBlock.BlockStatement, MethodStatementSyntax), typeSymbol)
            Dim endMethodStatement = methodBlock.EndBlockStatement

            If endMethodStatement IsNot Nothing AndAlso Not endMethodStatement.IsMissing Then
                ' Note that we don't have to remove/replace the trailing trivia for the end block statement
                ' because we're already doing that for the whole block.
                If endMethodStatement.IsKind(SyntaxKind.EndSubStatement) AndAlso typeSymbol IsNot Nothing Then
                    endMethodStatement = SyntaxFactory.EndFunctionStatement()
                ElseIf endMethodStatement.IsKind(SyntaxKind.EndFunctionStatement) AndAlso typeSymbol Is Nothing Then
                    endMethodStatement = SyntaxFactory.EndSubStatement()
                End If
            End If

            methodBlock = methodBlock.Update(If(methodStatement.Kind = SyntaxKind.SubStatement, SyntaxKind.SubBlock, SyntaxKind.FunctionBlock),
                                             methodStatement, methodBlock.Statements, endMethodStatement)

            Return methodBlock _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia)
        End Function

        Private Function SetParameterType(parameter As ParameterSyntax, typeSymbol As ITypeSymbol) As ParameterSyntax
            If typeSymbol Is Nothing Then
                Return parameter
            End If

            ' Remove the leading and trailing trivia and save it for reattachment later.\
            Dim leadingTrivia = parameter.GetLeadingTrivia()
            Dim trailingTrivia = parameter.GetTrailingTrivia()
            parameter = parameter _
                .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                .WithTrailingTrivia(SyntaxTriviaList.Empty)

            Dim typeName = typeSymbol.ToDisplayString(s_setTypeFormat)
            Dim newType = SyntaxFactory.ParseTypeName(typeName)

            If parameter.AsClause IsNot Nothing Then
                Debug.Assert(TypeOf parameter.AsClause Is SimpleAsClauseSyntax)

                Dim oldType = DirectCast(parameter.AsClause, SimpleAsClauseSyntax).Type
                parameter = parameter.ReplaceNode(oldType, newType)
            Else
                parameter = parameter.WithAsClause(SyntaxFactory.SimpleAsClause(newType))
            End If

            Return parameter _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia)
        End Function

        Private Function SetPropertyType(propertyStatement As PropertyStatementSyntax, typeSymbol As ITypeSymbol) As PropertyStatementSyntax
            If typeSymbol Is Nothing Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            ' Remove the leading and trailing trivia and save it for reattachment later.\
            Dim leadingTrivia = propertyStatement.GetLeadingTrivia()
            Dim trailingTrivia = propertyStatement.GetTrailingTrivia()
            propertyStatement = propertyStatement _
                .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                .WithTrailingTrivia(SyntaxTriviaList.Empty)

            Dim typeName = typeSymbol.ToDisplayString(s_setTypeFormat)
            Dim newType = SyntaxFactory.ParseTypeName(typeName)

            If propertyStatement.AsClause IsNot Nothing Then
                Dim oldType = propertyStatement.AsClause.Type()
                If oldType IsNot Nothing Then
                    propertyStatement = propertyStatement.ReplaceNode(oldType, newType)
                End If
            Else
                propertyStatement = propertyStatement.WithAsClause(SyntaxFactory.SimpleAsClause(newType))
            End If

            Return propertyStatement _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia)
        End Function

        Private Function SetPropertyType(propertyBlock As PropertyBlockSyntax, typeSymbol As ITypeSymbol) As PropertyBlockSyntax
            If typeSymbol Is Nothing Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            ' Remove the leading and trailing trivia and save it for reattachment later.\
            Dim leadingTrivia = propertyBlock.GetLeadingTrivia()
            Dim trailingTrivia = propertyBlock.GetTrailingTrivia()
            propertyBlock = propertyBlock _
                .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                .WithTrailingTrivia(SyntaxTriviaList.Empty)

            Dim propertyStatement = SetPropertyType(propertyBlock.PropertyStatement, typeSymbol)
            propertyBlock = propertyBlock.WithPropertyStatement(propertyStatement)

            Dim typeName = typeSymbol.ToDisplayString(s_setTypeFormat)
            Dim newType = SyntaxFactory.ParseTypeName(typeName)

            For i = 0 To propertyBlock.Accessors.Count - 1
                Dim accessorBlock = propertyBlock.Accessors(i)
                Dim newAccessorBlock = accessorBlock

                If accessorBlock.Kind = SyntaxKind.SetAccessorBlock Then
                    ' Update the first parameter of the SetAccessor statement
                    Dim firstParameter = accessorBlock.BlockStatement.ParameterList.Parameters.FirstOrDefault()
                    If firstParameter IsNot Nothing Then
                        Dim newFirstParameter As ParameterSyntax
                        If firstParameter.AsClause IsNot Nothing Then
                            Debug.Assert(TypeOf firstParameter.AsClause Is SimpleAsClauseSyntax)

                            Dim oldType = DirectCast(firstParameter.AsClause, SimpleAsClauseSyntax).Type
                            newFirstParameter = firstParameter.ReplaceNode(oldType, newType)
                        Else
                            newFirstParameter = firstParameter.WithAsClause(SyntaxFactory.SimpleAsClause(newType))
                        End If

                        newFirstParameter = newFirstParameter _
                            .WithLeadingTrivia(firstParameter.GetLeadingTrivia()) _
                            .WithTrailingTrivia(firstParameter.GetTrailingTrivia())

                        newAccessorBlock = accessorBlock.ReplaceNode(firstParameter, newFirstParameter)
                    End If
                End If

                If accessorBlock IsNot newAccessorBlock Then
                    propertyBlock = propertyBlock.ReplaceNode(accessorBlock, newAccessorBlock)
                End If
            Next

            Return propertyBlock _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia)
        End Function

        Private Function SetVariableType(variableDeclarator As VariableDeclaratorSyntax, typeSymbol As ITypeSymbol) As VariableDeclaratorSyntax
            If typeSymbol Is Nothing Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            ' Remove the leading and trailing trivia and save it for reattachment later.\
            Dim leadingTrivia = variableDeclarator.GetLeadingTrivia()
            Dim trailingTrivia = variableDeclarator.GetTrailingTrivia()
            variableDeclarator = variableDeclarator _
                .WithLeadingTrivia(SyntaxTriviaList.Empty) _
                .WithTrailingTrivia(SyntaxTriviaList.Empty)

            Dim typeName = typeSymbol.ToDisplayString(s_setTypeFormat)
            Dim newType = SyntaxFactory.ParseTypeName(typeName)

            If variableDeclarator.AsClause IsNot Nothing Then
                Dim oldType = variableDeclarator.AsClause.Type()
                If oldType IsNot Nothing Then
                    variableDeclarator = variableDeclarator.ReplaceNode(oldType, newType)
                End If
            Else
                variableDeclarator = variableDeclarator.WithAsClause(SyntaxFactory.SimpleAsClause(newType))
            End If

            Return variableDeclarator _
                .WithLeadingTrivia(leadingTrivia) _
                .WithTrailingTrivia(trailingTrivia)
        End Function

        Public Overrides Function SetType(node As SyntaxNode, typeSymbol As ITypeSymbol) As SyntaxNode
            Debug.Assert(TypeOf node Is DelegateStatementSyntax OrElse
                         TypeOf node Is EventStatementSyntax OrElse
                         TypeOf node Is EventBlockSyntax OrElse
                         TypeOf node Is DeclareStatementSyntax OrElse
                         TypeOf node Is MethodStatementSyntax OrElse
                         TypeOf node Is MethodBlockBaseSyntax OrElse
                         TypeOf node Is ParameterSyntax OrElse
                         TypeOf node Is PropertyStatementSyntax OrElse
                         TypeOf node Is PropertyBlockSyntax OrElse
                         TypeOf node Is VariableDeclaratorSyntax)

            If TypeOf node Is DelegateStatementSyntax Then
                Return SetDelegateType(DirectCast(node, DelegateStatementSyntax), typeSymbol)
            End If

            If TypeOf node Is EventStatementSyntax Then
                Return SetEventType(DirectCast(node, EventStatementSyntax), typeSymbol)
            End If

            If TypeOf node Is EventBlockSyntax Then
                Return SetEventType(DirectCast(node, EventBlockSyntax), typeSymbol)
            End If

            If TypeOf node Is DeclareStatementSyntax Then
                Return SetMethodType(DirectCast(node, DeclareStatementSyntax), typeSymbol)
            End If

            If TypeOf node Is MethodStatementSyntax Then
                Return SetMethodType(DirectCast(node, MethodStatementSyntax), typeSymbol)
            End If

            If TypeOf node Is MethodBlockSyntax Then
                Return SetMethodType(DirectCast(node, MethodBlockSyntax), typeSymbol)
            End If

            If TypeOf node Is ConstructorBlockSyntax Then
                Return node
            End If

            If TypeOf node Is OperatorBlockSyntax Then
                Return node
            End If

            If TypeOf node Is ParameterSyntax Then
                Return SetParameterType(DirectCast(node, ParameterSyntax), typeSymbol)
            End If

            If TypeOf node Is PropertyStatementSyntax Then
                Return SetPropertyType(DirectCast(node, PropertyStatementSyntax), typeSymbol)
            End If

            If TypeOf node Is PropertyBlockSyntax Then
                Return SetPropertyType(DirectCast(node, PropertyBlockSyntax), typeSymbol)
            End If

            If TypeOf node Is VariableDeclaratorSyntax Then
                Return SetVariableType(DirectCast(node, VariableDeclaratorSyntax), typeSymbol)
            End If

            Throw New NotImplementedException()
        End Function

        Public Overrides Function GetFullyQualifiedName(name As String, position As Integer, semanticModel As SemanticModel) As String
            Dim typeName = SyntaxFactory.ParseTypeName(name)
            If TypeOf typeName Is PredefinedTypeSyntax Then
                Dim predefinedType As PredefinedType
                If SyntaxFactsService.TryGetPredefinedType(DirectCast(typeName, PredefinedTypeSyntax).Keyword, predefinedType) Then
                    Dim specialType = predefinedType.ToSpecialType()
                    Return semanticModel.Compilation.GetSpecialType(specialType).GetEscapedFullName()
                End If
            Else
                Dim symbols = semanticModel.LookupNamespacesAndTypes(position, name:=name)
                If symbols.Length > 0 Then
                    Return symbols(0).GetEscapedFullName()
                End If
            End If

            Return name
        End Function

        Public Overrides Function GetInitExpression(node As SyntaxNode) As String
            Dim initializer As ExpressionSyntax = Nothing

            Select Case node.Kind
                Case SyntaxKind.Parameter
                    Dim parameter = DirectCast(node, ParameterSyntax)
                    If parameter.Default IsNot Nothing Then
                        initializer = parameter.Default.Value
                    End If

                Case SyntaxKind.ModifiedIdentifier
                    Dim modifiedIdentifier = DirectCast(node, ModifiedIdentifierSyntax)
                    Dim variableDeclarator = DirectCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
                    initializer = variableDeclarator.GetInitializer()

                Case SyntaxKind.EnumMemberDeclaration
                    Dim enumMemberDeclaration = DirectCast(node, EnumMemberDeclarationSyntax)
                    If enumMemberDeclaration.Initializer IsNot Nothing Then
                        initializer = enumMemberDeclaration.Initializer.Value
                    End If
            End Select

            Return If(initializer IsNot Nothing,
                      initializer.ToString(),
                      Nothing)
        End Function

        Public Overrides Function AddInitExpression(node As SyntaxNode, value As String) As SyntaxNode
            Select Case node.Kind
                Case SyntaxKind.Parameter
                    Dim parameter = DirectCast(node, ParameterSyntax)

                    Dim parameterKind = GetParameterKind(parameter)
                    If Not (parameterKind And EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional) <> 0 Then
                        Throw Exceptions.ThrowEInvalidArg
                    End If

                    If String.IsNullOrWhiteSpace(value) Then
                        ' Remove the Optional modifier
                        parameterKind = parameterKind And Not EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional
                        parameter = DirectCast(SetParameterKind(parameter, parameterKind), ParameterSyntax)

                        If parameter.Default IsNot Nothing Then
                            parameter = parameter.RemoveNode(parameter.Default, SyntaxRemoveOptions.KeepNoTrivia)
                        End If

                        Return parameter
                    End If

                    Dim expression = SyntaxFactory.ParseExpression(value)

                    Dim equalsValueClause = If(parameter.Default IsNot Nothing AndAlso Not parameter.Default.IsMissing,
                                               parameter.Default.WithValue(expression),
                                               SyntaxFactory.EqualsValue(expression))

                    Return parameter.WithDefault(equalsValueClause)

                Case SyntaxKind.VariableDeclarator
                    Dim variableDeclarator = DirectCast(node, VariableDeclaratorSyntax)

                    If String.IsNullOrWhiteSpace(value) Then
                        If variableDeclarator.Initializer IsNot Nothing Then
                            variableDeclarator = variableDeclarator.RemoveNode(variableDeclarator.Initializer, SyntaxRemoveOptions.KeepExteriorTrivia)
                        End If

                        Return variableDeclarator
                    End If

                    Dim trailingTrivia = variableDeclarator.GetTrailingTrivia()
                    variableDeclarator = variableDeclarator.WithTrailingTrivia(SyntaxTriviaList.Empty)

                    Dim expression = SyntaxFactory.ParseExpression(value)

                    Dim equalsValueClause = If(variableDeclarator.Initializer IsNot Nothing AndAlso Not variableDeclarator.Initializer.IsMissing,
                                               variableDeclarator.Initializer.WithValue(expression),
                                               SyntaxFactory.EqualsValue(expression)).WithTrailingTrivia(trailingTrivia)

                    Return variableDeclarator.WithInitializer(equalsValueClause)

                Case SyntaxKind.EnumMemberDeclaration
                    Dim enumMemberDeclaration = DirectCast(node, EnumMemberDeclarationSyntax)

                    If String.IsNullOrWhiteSpace(value) Then
                        If enumMemberDeclaration.Initializer IsNot Nothing Then
                            enumMemberDeclaration = enumMemberDeclaration.RemoveNode(enumMemberDeclaration.Initializer, SyntaxRemoveOptions.KeepExteriorTrivia)
                        End If

                        Return enumMemberDeclaration
                    End If

                    Dim trailingTrivia = enumMemberDeclaration.GetTrailingTrivia()
                    enumMemberDeclaration = enumMemberDeclaration.WithTrailingTrivia(SyntaxTriviaList.Empty)

                    Dim expression = SyntaxFactory.ParseExpression(value)

                    Dim equalsValueClause = If(enumMemberDeclaration.Initializer IsNot Nothing AndAlso Not enumMemberDeclaration.Initializer.IsMissing,
                                               enumMemberDeclaration.Initializer.WithValue(expression),
                                               SyntaxFactory.EqualsValue(expression)).WithTrailingTrivia(trailingTrivia)

                    Return enumMemberDeclaration.WithInitializer(equalsValueClause)

                Case Else
                    Throw Exceptions.ThrowEFail()
            End Select
        End Function

        Public Overrides Function GetDestination(containerNode As SyntaxNode) As CodeGenerationDestination
            Return VisualBasicCodeGenerationHelpers.GetDestination(containerNode)
        End Function

        Protected Overrides Function GetDefaultAccessibility(targetSymbolKind As SymbolKind, destination As CodeGenerationDestination) As Accessibility
            If destination = CodeGenerationDestination.StructType Then
                Return Accessibility.Public
            End If

            Select Case targetSymbolKind
                Case SymbolKind.Field
                    Return Accessibility.Private
                Case SymbolKind.Method,
                     SymbolKind.Property,
                     SymbolKind.Event,
                     SymbolKind.NamedType
                    Return Accessibility.Public
                Case Else
                    Debug.Fail("Invalid symbol kind: " & targetSymbolKind)
                    Throw Exceptions.ThrowEFail()
            End Select
        End Function

        Public Overrides Function GetTypeSymbolFromFullName(fullName As String, compilation As Compilation) As ITypeSymbol
            Dim typeSymbol As ITypeSymbol = compilation.GetTypeByMetadataName(fullName)

            If typeSymbol Is Nothing Then
                Dim parsedTypeName = SyntaxFactory.ParseTypeName(fullName)

                ' Check to see if the name we parsed has any skipped text. If it does, don't bother trying to
                ' speculatively bind it because we'll likely just get the wrong thing since we found a bunch
                ' of non-sensical tokens.

                ' NOTE: There appears to be a VB parser issue where "ContainsSkippedText" does not return true
                ' even when there is clearly skipped token trivia present. We work around this by for a particularly
                ' common case by checking whether the trailing trivia contains any skipped token trivia.
                ' https://github.com/dotnet/roslyn/issues/7182 has been filed for the parser issue.

                If parsedTypeName.ContainsSkippedText OrElse
                   parsedTypeName.GetTrailingTrivia().Any(SyntaxKind.SkippedTokensTrivia) Then

                    Return Nothing
                End If

                ' If we couldn't get the name, we just grab the first tree in the compilation to
                ' speculatively bind at position zero. However, if there *aren't* any trees, we fork the
                ' compilation with an empty tree for the purposes of speculative binding.
                '
                ' I'm a bad person.
                Dim tree = compilation.SyntaxTrees.FirstOrDefault()
                If tree Is Nothing Then
                    tree = SyntaxFactory.ParseSyntaxTree("")
                    compilation = compilation.AddSyntaxTrees(tree)
                End If

                Dim semanticModel = compilation.GetSemanticModel(tree)
                typeSymbol = semanticModel.GetSpeculativeTypeInfo(0, parsedTypeName, SpeculativeBindingOption.BindAsTypeOrNamespace).Type
            End If

            If typeSymbol Is Nothing Then
                Debug.Fail("Could not find type: " & fullName)
                Throw New ArgumentException()
            End If

            Return typeSymbol
        End Function

        Protected Overrides Function GetTypeSymbolFromPartialName(partialName As String, semanticModel As SemanticModel, position As Integer) As ITypeSymbol
            Dim parsedTypeName = SyntaxFactory.ParseTypeName(partialName)

            Return semanticModel.GetSpeculativeTypeInfo(position, parsedTypeName, SpeculativeBindingOption.BindAsTypeOrNamespace).Type
        End Function

        Public Overrides Function CreateReturnDefaultValueStatement(type As ITypeSymbol) As SyntaxNode
            Return SyntaxFactory.ReturnStatement(
                SyntaxFactory.NothingLiteralExpression(
                    SyntaxFactory.Token(SyntaxKind.NothingKeyword)))
        End Function

        Protected Overrides Function IsCodeModelNode(node As SyntaxNode) As Boolean
            Select Case CType(node.Kind, SyntaxKind)
                Case SyntaxKind.ClassBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.DeclareFunctionStatement,
                     SyntaxKind.DeclareSubStatement,
                     SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement,
                     SyntaxKind.EnumBlock,
                     SyntaxKind.EnumMemberDeclaration,
                     SyntaxKind.EventBlock,
                     SyntaxKind.EventStatement,
                     SyntaxKind.FieldDeclaration,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.ModuleBlock,
                     SyntaxKind.NamespaceBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.PropertyBlock,
                     SyntaxKind.StructureBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.SimpleImportsClause
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Protected Overrides Function GetFieldFromVariableNode(variableNode As SyntaxNode) As SyntaxNode
            Return If(variableNode.Kind = SyntaxKind.ModifiedIdentifier,
                      variableNode.FirstAncestorOrSelf(Of FieldDeclarationSyntax)(),
                      variableNode)
        End Function

        Protected Overrides Function GetVariableFromFieldNode(fieldNode As SyntaxNode) As SyntaxNode
            ' Work around that the fact that VB code model really deals with fields as modified identifiers
            Return If(TypeOf fieldNode Is FieldDeclarationSyntax,
                      DirectCast(fieldNode, FieldDeclarationSyntax).Declarators.Single().Names.Single(),
                      fieldNode)
        End Function

        Protected Overrides Function GetAttributeFromAttributeDeclarationNode(node As SyntaxNode) As SyntaxNode
            Return If(TypeOf node Is AttributeListSyntax,
                      DirectCast(node, AttributeListSyntax).Attributes.First,
                      node)
        End Function

        Protected Overrides Function GetSpanToFormat(root As SyntaxNode, span As TextSpan) As TextSpan
            Dim startToken = GetTokenWithoutAnnotation(root.FindToken(span.Start).GetPreviousToken(), Function(t) t.GetPreviousToken())
            Dim endToken = GetTokenWithoutAnnotation(root.FindToken(span.End), Function(t) t.GetNextToken())

            Return GetEncompassingSpan(root, startToken, endToken)
        End Function

        Protected Overrides Function InsertMemberNodeIntoContainer(index As Integer, member As SyntaxNode, container As SyntaxNode) As SyntaxNode
            Dim declarationStatement = DirectCast(member, DeclarationStatementSyntax)

            If TypeOf container Is CompilationUnitSyntax Then
                Dim compilationUnit = DirectCast(container, CompilationUnitSyntax)

                Return compilationUnit.WithMembers(compilationUnit.Members.Insert(index, declarationStatement))
            ElseIf TypeOf container Is NamespaceBlockSyntax Then
                Dim namespaceBlock = DirectCast(container, NamespaceBlockSyntax)

                Return namespaceBlock.WithMembers(namespaceBlock.Members.Insert(index, declarationStatement))
            ElseIf TypeOf container Is ClassBlockSyntax Then
                Dim classBlock = DirectCast(container, ClassBlockSyntax)

                Return classBlock.WithMembers(classBlock.Members.Insert(index, declarationStatement))
            ElseIf TypeOf container Is InterfaceBlockSyntax Then
                Dim interfaceBlock = DirectCast(container, InterfaceBlockSyntax)

                Return interfaceBlock.WithMembers(interfaceBlock.Members.Insert(index, declarationStatement))
            ElseIf TypeOf container Is StructureBlockSyntax Then
                Dim structureBlock = DirectCast(container, StructureBlockSyntax)

                Return structureBlock.WithMembers(structureBlock.Members.Insert(index, declarationStatement))
            ElseIf TypeOf container Is ModuleBlockSyntax Then
                Dim moduleBlock = DirectCast(container, ModuleBlockSyntax)

                Return moduleBlock.WithMembers(moduleBlock.Members.Insert(index, declarationStatement))
            ElseIf TypeOf container Is EnumBlockSyntax Then
                Dim enumBlock = DirectCast(container, EnumBlockSyntax)

                Return enumBlock.WithMembers(enumBlock.Members.Insert(index, declarationStatement))
            End If

            Throw Exceptions.ThrowEFail()
        End Function

        Private Shared Function GetMember(container As SyntaxNode, index As Integer) As StatementSyntax
            If TypeOf container Is CompilationUnitSyntax Then
                Return DirectCast(container, CompilationUnitSyntax).Members(index)
            ElseIf TypeOf container Is NamespaceBlockSyntax Then
                Return DirectCast(container, NamespaceBlockSyntax).Members(index)
            ElseIf TypeOf container Is ClassBlockSyntax Then
                Return DirectCast(container, ClassBlockSyntax).Members(index)
            ElseIf TypeOf container Is InterfaceBlockSyntax Then
                Return DirectCast(container, InterfaceBlockSyntax).Members(index)
            ElseIf TypeOf container Is StructureBlockSyntax Then
                Return DirectCast(container, StructureBlockSyntax).Members(index)
            ElseIf TypeOf container Is ModuleBlockSyntax Then
                Return DirectCast(container, ModuleBlockSyntax).Members(index)
            ElseIf TypeOf container Is EnumBlockSyntax Then
                Return DirectCast(container, EnumBlockSyntax).Members(index)
            End If

            Throw Exceptions.ThrowEFail()
        End Function

        Private Shared Function GetAttribute(container As SyntaxNode, index As Integer) As AttributeListSyntax
            If TypeOf container Is CompilationUnitSyntax Then
                Dim compilationUnit = DirectCast(container, CompilationUnitSyntax).Attributes(index).AttributeLists(0)
            ElseIf TypeOf container Is TypeBlockSyntax Then
                Return DirectCast(container, TypeBlockSyntax).BlockStatement.AttributeLists(index)
            ElseIf TypeOf container Is EnumMemberDeclarationSyntax Then
                Return DirectCast(container, EnumMemberDeclarationSyntax).AttributeLists(index)
            ElseIf TypeOf container Is MethodBlockBaseSyntax Then
                Return DirectCast(container, MethodBlockBaseSyntax).BlockStatement.AttributeLists(index)
            ElseIf TypeOf container Is PropertyBlockSyntax Then
                Return DirectCast(container, PropertyBlockSyntax).PropertyStatement.AttributeLists(index)
            ElseIf TypeOf container Is FieldDeclarationSyntax Then
                Return DirectCast(container, FieldDeclarationSyntax).AttributeLists(index)
            ElseIf TypeOf container Is ParameterSyntax Then
                Return DirectCast(container, ParameterSyntax).AttributeLists(index)
            End If

            Throw Exceptions.ThrowEFail()
        End Function

        Protected Overrides Function InsertAttributeArgumentIntoContainer(index As Integer, attributeArgument As SyntaxNode, container As SyntaxNode) As SyntaxNode
            If TypeOf container Is AttributeSyntax Then
                Dim attribute = DirectCast(container, AttributeSyntax)
                Dim argumentList = attribute.ArgumentList

                Dim newArgumentList As ArgumentListSyntax

                If argumentList Is Nothing Then
                    newArgumentList = SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            DirectCast(attributeArgument, ArgumentSyntax)))
                Else
                    Dim newArguments = argumentList.Arguments.Insert(index, DirectCast(attributeArgument, ArgumentSyntax))
                    newArgumentList = argumentList.WithArguments(newArguments)
                End If

                Return attribute.WithArgumentList(newArgumentList)
            End If

            Throw Exceptions.ThrowEFail()
        End Function

        Private Function InsertAttributeListInto(attributes As SyntaxList(Of AttributesStatementSyntax), index As Integer, attribute As AttributesStatementSyntax) As SyntaxList(Of AttributesStatementSyntax)
            ' we need to explicitly add end of line trivia here since both of them (with or without) are valid but parsed differently
            If index = 0 Then
                Return attributes.Insert(index, attribute)
            End If

            Dim previousAttribute = attributes(index - 1)
            If previousAttribute.GetTrailingTrivia().Any(Function(t) t.Kind = SyntaxKind.EndOfLineTrivia) Then
                Return attributes.Insert(index, attribute)
            End If

            attributes = attributes.Replace(previousAttribute, previousAttribute.WithAppendedTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))
            Return attributes.Insert(index, attribute)
        End Function

        Protected Overrides Function InsertAttributeListIntoContainer(index As Integer, list As SyntaxNode, container As SyntaxNode) As SyntaxNode
            ' If the attribute is being inserted at the first index and the container is not the compilation unit, copy leading trivia
            ' to the attribute that is being inserted.
            If index = 0 AndAlso TypeOf container IsNot CompilationUnitSyntax Then
                Dim firstToken = container.GetFirstToken()
                If firstToken.HasLeadingTrivia Then
                    Dim trivia = firstToken.LeadingTrivia

                    container = container.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(SyntaxTriviaList.Empty))
                    list = list.WithLeadingTrivia(trivia)
                End If
            End If

            ' If the attribute to be inserted does not have a trailing line break, add one (unless this is a parameter).
            If TypeOf container IsNot ParameterSyntax AndAlso
               (Not list.HasTrailingTrivia OrElse Not list.GetTrailingTrivia().Any(SyntaxKind.EndOfLineTrivia)) Then

                list = list.WithAppendedTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            End If

            Dim attributeList = DirectCast(list, AttributeListSyntax)

            If TypeOf container Is CompilationUnitSyntax Then
                Dim compilationUnit = DirectCast(container, CompilationUnitSyntax)
                Dim attributesStatement = SyntaxFactory.AttributesStatement(SyntaxFactory.SingletonList(attributeList))
                Dim attributeStatements = InsertAttributeListInto(compilationUnit.Attributes, index, attributesStatement)
                Return compilationUnit.WithAttributes(attributeStatements)
            ElseIf TypeOf container Is ModuleBlockSyntax Then
                Dim moduleBlock = DirectCast(container, ModuleBlockSyntax)
                Dim attributeLists = moduleBlock.ModuleStatement.AttributeLists.Insert(index, attributeList)
                Return moduleBlock.WithBlockStatement(moduleBlock.ModuleStatement.WithAttributeLists(attributeLists))
            ElseIf TypeOf container Is StructureBlockSyntax Then
                Dim structureBlock = DirectCast(container, StructureBlockSyntax)
                Dim attributeLists = structureBlock.StructureStatement.AttributeLists.Insert(index, attributeList)
                Return structureBlock.WithStructureStatement(structureBlock.StructureStatement.WithAttributeLists(attributeLists))
            ElseIf TypeOf container Is InterfaceBlockSyntax Then
                Dim interfaceBlock = DirectCast(container, InterfaceBlockSyntax)
                Dim attributeLists = interfaceBlock.InterfaceStatement.AttributeLists.Insert(index, attributeList)
                Return interfaceBlock.WithInterfaceStatement(interfaceBlock.InterfaceStatement.WithAttributeLists(attributeLists))
            ElseIf TypeOf container Is ClassBlockSyntax Then
                Dim classBlock = DirectCast(container, ClassBlockSyntax)
                Dim attributeLists = classBlock.ClassStatement.AttributeLists.Insert(index, attributeList)
                Dim begin = classBlock.ClassStatement.WithAttributeLists(attributeLists)
                Return classBlock.WithClassStatement(begin)
            ElseIf TypeOf container Is EnumBlockSyntax Then
                Dim enumBlock = DirectCast(container, EnumBlockSyntax)
                Dim attributeLists = enumBlock.EnumStatement.AttributeLists.Insert(index, attributeList)
                Return enumBlock.WithEnumStatement(enumBlock.EnumStatement.WithAttributeLists(attributeLists))
            ElseIf TypeOf container Is EnumMemberDeclarationSyntax Then
                Dim enumMember = DirectCast(container, EnumMemberDeclarationSyntax)
                Dim attributeLists = enumMember.AttributeLists.Insert(index, attributeList)
                Return enumMember.WithAttributeLists(attributeLists)
            ElseIf TypeOf container Is DelegateStatementSyntax Then
                Dim delegateStatement = DirectCast(container, DelegateStatementSyntax)
                Dim attributeLists = delegateStatement.AttributeLists.Insert(index, attributeList)
                Return delegateStatement.WithAttributeLists(attributeLists)
            ElseIf TypeOf container Is DeclareStatementSyntax Then
                Dim declareStatement = DirectCast(container, DeclareStatementSyntax)
                Dim attributeLists = declareStatement.AttributeLists.Insert(index, attributeList)
                Return declareStatement.WithAttributeLists(attributeLists)
            ElseIf TypeOf container Is MethodStatementSyntax Then
                Dim methodStatement = DirectCast(container, MethodStatementSyntax)
                Dim attributeLists = methodStatement.AttributeLists.Insert(index, attributeList)
                Return methodStatement.WithAttributeLists(attributeLists)
            ElseIf TypeOf container Is MethodBlockBaseSyntax Then
                Dim method = DirectCast(container, MethodBlockBaseSyntax)
                If TypeOf method.BlockStatement Is SubNewStatementSyntax Then
                    Dim constructor = DirectCast(method.BlockStatement, SubNewStatementSyntax)
                    Dim attributeLists = constructor.AttributeLists.Insert(index, attributeList)
                    Return DirectCast(method, ConstructorBlockSyntax).WithBlockStatement(constructor.WithAttributeLists(attributeLists))
                ElseIf TypeOf method.BlockStatement Is OperatorStatementSyntax Then
                    Dim operatorStatement = DirectCast(method.BlockStatement, OperatorStatementSyntax)
                    Dim attributeLists = operatorStatement.AttributeLists.Insert(index, attributeList)
                    Return DirectCast(method, OperatorBlockSyntax).WithBlockStatement(operatorStatement.WithAttributeLists(attributeLists))
                ElseIf TypeOf method.BlockStatement Is MethodStatementSyntax Then
                    Dim methodStatement = DirectCast(method.BlockStatement, MethodStatementSyntax)
                    Dim attributeLists = methodStatement.AttributeLists.Insert(index, attributeList)
                    Return DirectCast(method, MethodBlockSyntax).WithBlockStatement(methodStatement.WithAttributeLists(attributeLists))
                End If
            ElseIf TypeOf container Is PropertyStatementSyntax Then
                Dim propertyStatement = DirectCast(container, PropertyStatementSyntax)
                Dim attributeLists = propertyStatement.AttributeLists.Insert(index, attributeList)
                Return propertyStatement.WithAttributeLists(attributeLists)
            ElseIf TypeOf container Is PropertyBlockSyntax Then
                Dim propertyBlock = DirectCast(container, PropertyBlockSyntax)
                Dim attributeLists = propertyBlock.PropertyStatement.AttributeLists.Insert(index, attributeList)
                Return propertyBlock.WithPropertyStatement(propertyBlock.PropertyStatement.WithAttributeLists(attributeLists))
            ElseIf TypeOf container Is EventStatementSyntax Then
                Dim eventStatement = DirectCast(container, EventStatementSyntax)
                Dim attributeLists = eventStatement.AttributeLists.Insert(index, attributeList)
                Return eventStatement.WithAttributeLists(attributeLists)
            ElseIf TypeOf container Is EventBlockSyntax Then
                Dim eventBlock = DirectCast(container, EventBlockSyntax)
                Dim attributeLists = eventBlock.EventStatement.AttributeLists.Insert(index, attributeList)
                Return eventBlock.WithEventStatement(eventBlock.EventStatement.WithAttributeLists(attributeLists))
            ElseIf TypeOf container Is FieldDeclarationSyntax Then
                Dim field = DirectCast(container, FieldDeclarationSyntax)
                Dim attributeLists = field.AttributeLists.Insert(index, attributeList)
                Return field.WithAttributeLists(attributeLists)
            ElseIf TypeOf container Is ParameterSyntax Then
                Dim parameter = DirectCast(container, ParameterSyntax)
                Dim attributeLists = parameter.AttributeLists.Insert(index, attributeList)
                Return parameter.WithAttributeLists(attributeLists)
            End If

            Throw Exceptions.ThrowEUnexpected()
        End Function

        Protected Overrides Function InsertImportIntoContainer(index As Integer, importNode As SyntaxNode, container As SyntaxNode) As SyntaxNode
            If TypeOf container IsNot CompilationUnitSyntax Then
                Throw Exceptions.ThrowEUnexpected()
            End If

            Dim compilationUnit = DirectCast(container, CompilationUnitSyntax)
            Dim importsStatement = DirectCast(importNode, ImportsStatementSyntax)

            Dim lastToken = importsStatement.GetLastToken()
            If lastToken.Kind <> SyntaxKind.EndOfLineTrivia Then
                importsStatement = importsStatement.ReplaceToken(lastToken, lastToken.WithAppendedTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))
            End If

            Dim importsList = compilationUnit.Imports.Insert(index, importsStatement)
            Return compilationUnit.WithImports(importsList)
        End Function

        Private Function InsertParameterIntoParameterList(index As Integer, parameter As ParameterSyntax, list As ParameterListSyntax) As ParameterListSyntax
            If list Is Nothing Then
                Return SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(parameter)) _
                    .WithAdditionalAnnotations(Formatter.Annotation)
            End If

            Dim parameters = list.Parameters.Insert(index, parameter)
            Return list.WithParameters(parameters)
        End Function

        Protected Overrides Function InsertParameterIntoContainer(index As Integer, parameter As SyntaxNode, container As SyntaxNode) As SyntaxNode
            Dim parameterNode = DirectCast(parameter, ParameterSyntax)

            If TypeOf container Is MethodBaseSyntax Then
                Dim methodStatement = DirectCast(container, MethodBaseSyntax)
                Dim parameterList = InsertParameterIntoParameterList(index, parameterNode, methodStatement.ParameterList)
                Return methodStatement.WithParameterList(parameterList)
            ElseIf TypeOf container Is MethodBlockBaseSyntax Then
                Dim methodBlock = DirectCast(container, MethodBlockBaseSyntax)
                Dim methodStatement = methodBlock.BlockStatement
                Dim parameterList = InsertParameterIntoParameterList(index, parameterNode, methodStatement.ParameterList)
                methodStatement = methodStatement.WithParameterList(parameterList)

                Select Case methodBlock.Kind
                    Case SyntaxKind.SubBlock, SyntaxKind.FunctionBlock
                        Return DirectCast(methodBlock, MethodBlockSyntax).WithBlockStatement(DirectCast(methodStatement, MethodStatementSyntax))
                    Case SyntaxKind.ConstructorBlock
                        Return DirectCast(methodBlock, ConstructorBlockSyntax).WithBlockStatement(DirectCast(methodStatement, SubNewStatementSyntax))
                    Case SyntaxKind.OperatorBlock
                        Return DirectCast(methodBlock, OperatorBlockSyntax).WithBlockStatement(DirectCast(methodStatement, OperatorStatementSyntax))
                    Case Else
                        Return DirectCast(methodBlock, AccessorBlockSyntax).WithBlockStatement(DirectCast(methodStatement, AccessorStatementSyntax))
                End Select
            ElseIf TypeOf container Is PropertyBlockSyntax Then
                Dim propertyBlock = DirectCast(container, PropertyBlockSyntax)
                Dim propertyStatement = propertyBlock.PropertyStatement
                Dim parameterList = InsertParameterIntoParameterList(index, parameterNode, propertyStatement.ParameterList)
                propertyStatement = propertyStatement.WithParameterList(parameterList)
                Return propertyBlock.WithPropertyStatement(propertyStatement)
            End If

            Throw Exceptions.ThrowEUnexpected()
        End Function

        Public Overrides Function IsNamespace(node As SyntaxNode) As Boolean
            Return TypeOf node Is NamespaceBlockSyntax
        End Function

        Public Overrides Function IsType(node As SyntaxNode) As Boolean
            Return TypeOf node Is TypeBlockSyntax OrElse
                   TypeOf node Is EnumBlockSyntax
        End Function

        Public Overrides Function GetMethodXml(node As SyntaxNode, semanticModel As SemanticModel) As String
            Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
            If methodBlock Is Nothing Then
                Throw Exceptions.ThrowEUnexpected()
            End If

            Return MethodXmlBuilder.Generate(methodBlock, semanticModel)
        End Function

        Private Shared Function GetMethodStatement(method As SyntaxNode) As MethodStatementSyntax
            Dim methodBlock = TryCast(method, MethodBlockBaseSyntax)
            If methodBlock Is Nothing Then
                Return Nothing
            End If

            Return TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
        End Function

        Private Overloads Shared Function GetHandledEventNames(methodStatement As MethodStatementSyntax) As IList(Of String)
            If methodStatement Is Nothing OrElse
               methodStatement.HandlesClause Is Nothing OrElse
               methodStatement.HandlesClause.Events.Count = 0 Then
                Return SpecializedCollections.EmptyList(Of String)()
            End If

            Dim eventCount = methodStatement.HandlesClause.Events.Count
            Dim result(eventCount - 1) As String
            For i = 0 To eventCount - 1
                Dim handlesItem = methodStatement.HandlesClause.Events(i)
                result(i) = handlesItem.EventContainer.ToString() & "."c & handlesItem.EventMember.ToString()
            Next

            Return result
        End Function

        Private Shared Function EscapeIfNotMeMyBaseOrMyClass(identifier As String) As String
            Select Case SyntaxFacts.GetKeywordKind(identifier)
                Case SyntaxKind.MeKeyword,
                     SyntaxKind.MyBaseKeyword,
                     SyntaxKind.MyClassKeyword,
                     SyntaxKind.None

                    Return identifier

                Case Else
                    Return "["c & identifier & "]"c
            End Select
        End Function

        Private Shared Function MakeHandledEventName(parentName As String, eventName As String) As String
            If eventName.Length >= parentName.Length Then
                If CaseInsensitiveComparison.Equals(eventName.Substring(0, parentName.Length), parentName) Then
                    Return "MyBase" & eventName.Substring(parentName.Length)
                End If
            End If

            ' If eventName starts with an unescaped keyword other than Me, MyBase or MyClass, we need to escape it.
            If Not eventName.StartsWith("[", StringComparison.Ordinal) Then
                Dim dotIndex = eventName.IndexOf("."c)
                If dotIndex >= 0 Then
                    Return EscapeIfNotMeMyBaseOrMyClass(eventName.Substring(0, dotIndex)) & eventName.Substring(dotIndex)
                Else
                    EscapeIfNotMeMyBaseOrMyClass(eventName)
                End If
            End If

            Return eventName
        End Function

        Public Overrides Function AddHandlesClause(document As Document, eventName As String, method As SyntaxNode, cancellationToken As CancellationToken) As Document
            Dim methodStatement = GetMethodStatement(method)

            Dim parentTypeBlock = TryCast(method.Parent, TypeBlockSyntax)
            If parentTypeBlock Is Nothing Then
                Throw Exceptions.ThrowEUnexpected()
            End If

            Dim parentName = parentTypeBlock.BlockStatement.Identifier.ToString()
            Dim newEventName = MakeHandledEventName(parentName, eventName)

            Dim position As Integer
            Dim textToInsert As String
            If methodStatement.HandlesClause Is Nothing Then
                position = methodStatement.ParameterList.CloseParenToken.Span.End
                textToInsert = " Handles " & newEventName
            Else
                position = methodStatement.HandlesClause.Span.End
                textToInsert = ", " & newEventName
            End If

            Dim text = document.GetTextAsync(cancellationToken) _
                               .WaitAndGetResult(cancellationToken)

            text = text.Replace(position, 0, textToInsert)

            Return document.WithText(text)
        End Function

        Public Overrides Function RemoveHandlesClause(document As Document, eventName As String, method As SyntaxNode, cancellationToken As CancellationToken) As Document
            Dim methodStatement = GetMethodStatement(method)

            Dim parentTypeBlock = TryCast(method.Parent, TypeBlockSyntax)
            If parentTypeBlock Is Nothing Then
                Throw Exceptions.ThrowEUnexpected()
            End If

            Dim parentName = parentTypeBlock.BlockStatement.Identifier.ToString()
            Dim newEventName = MakeHandledEventName(parentName, eventName)

            If methodStatement.HandlesClause Is Nothing Then
                Throw Exceptions.ThrowEUnexpected()
            End If

            Dim indexOfDot = newEventName.IndexOf("."c)
            If indexOfDot = -1 Then
                Throw Exceptions.ThrowEUnexpected()
            End If

            Dim containerName = newEventName.Substring(0, indexOfDot)
            Dim memberName = newEventName.Substring(indexOfDot + 1)

            Dim clauseItemToRemove As HandlesClauseItemSyntax = Nothing
            For Each handlesClauseItem In methodStatement.HandlesClause.Events
                If handlesClauseItem.EventContainer.ToString() = containerName AndAlso
                    handlesClauseItem.EventMember.ToString() = memberName Then

                    clauseItemToRemove = handlesClauseItem
                    Exit For
                End If
            Next

            If clauseItemToRemove Is Nothing Then
                Throw Exceptions.ThrowEUnexpected()
            End If

            Dim text = document.GetTextAsync(cancellationToken) _
                               .WaitAndGetResult(cancellationToken)

            If methodStatement.HandlesClause.Events.Count = 1 Then
                ' Easy case, delete the whole clause
                text = text.Replace(methodStatement.HandlesClause.Span, String.Empty)
            Else
                ' Harder case, remove it from the list.  If it's the first one, remove the following
                ' comma, else remove the preceding comma.
                Dim index = methodStatement.HandlesClause.Events.IndexOf(clauseItemToRemove)
                If index = 0 Then
                    text = text.Replace(TextSpan.FromBounds(clauseItemToRemove.SpanStart, methodStatement.HandlesClause.Events.GetSeparator(0).Span.End), String.Empty)
                Else
                    text = text.Replace(TextSpan.FromBounds(methodStatement.HandlesClause.Events.GetSeparator(index - 1).SpanStart, clauseItemToRemove.Span.End), String.Empty)
                End If
            End If

            Return document.WithText(text)
        End Function

        Public Overloads Overrides Function GetHandledEventNames(method As SyntaxNode, semanticModel As SemanticModel) As IList(Of String)
            Dim methodStatement = GetMethodStatement(method)

            Return GetHandledEventNames(methodStatement)
        End Function

        Public Overrides Function HandlesEvent(eventName As String, method As SyntaxNode, semanticModel As SemanticModel) As Boolean
            Dim methodStatement = GetMethodStatement(method)
            Dim handledEventNames = GetHandledEventNames(methodStatement)

            For Each handledEventName In handledEventNames
                If CaseInsensitiveComparison.Equals(eventName, handledEventName) Then
                    Return True
                End If
            Next

            Return False
        End Function

        Public Overrides Function GetFunctionExtenderNames() As String()
            Return {ExtenderNames.VBPartialMethodExtender}
        End Function

        Public Overrides Function GetFunctionExtender(name As String, node As SyntaxNode, symbol As ISymbol) As Object
            If Not TypeOf node Is MethodBlockBaseSyntax AndAlso
               Not TypeOf node Is MethodStatementSyntax AndAlso
               Not TypeOf symbol Is IMethodSymbol Then

                Throw Exceptions.ThrowEUnexpected()
            End If

            If StringComparer.OrdinalIgnoreCase.Equals(name, ExtenderNames.VBPartialMethodExtender) Then
                Dim methodSymbol = DirectCast(symbol, IMethodSymbol)
                Dim isPartial = methodSymbol.PartialDefinitionPart IsNot Nothing OrElse methodSymbol.PartialImplementationPart IsNot Nothing
                Dim isDeclaration = If(isPartial,
                                       methodSymbol.PartialDefinitionPart Is Nothing,
                                       False)

                Return PartialMethodExtender.Create(isDeclaration, isPartial)
            End If

            Throw Exceptions.ThrowEFail()
        End Function

        Public Overrides Function GetPropertyExtenderNames() As String()
            Return {ExtenderNames.VBAutoPropertyExtender}
        End Function

        Public Overrides Function GetPropertyExtender(name As String, node As SyntaxNode, symbol As ISymbol) As Object
            If Not TypeOf node Is PropertyBlockSyntax AndAlso
               Not TypeOf node Is PropertyStatementSyntax AndAlso
               Not TypeOf symbol Is IPropertySymbol Then

                Throw Exceptions.ThrowEUnexpected()
            End If

            If StringComparer.OrdinalIgnoreCase.Equals(name, ExtenderNames.VBAutoPropertyExtender) Then
                Dim isAutoImplemented = TypeOf node Is PropertyStatementSyntax AndAlso
                                        Not TypeOf node.Parent Is InterfaceBlockSyntax

                Return AutoPropertyExtender.Create(isAutoImplemented)
            End If

            Throw Exceptions.ThrowEFail()
        End Function

        Public Overrides Function GetExternalTypeExtenderNames() As String()
            Return {ExtenderNames.ExternalLocation}
        End Function

        Public Overrides Function GetExternalTypeExtender(name As String, externalLocation As String) As Object
            Debug.Assert(externalLocation IsNot Nothing)

            If StringComparer.OrdinalIgnoreCase.Equals(name, ExtenderNames.ExternalLocation) Then
                Return CodeTypeLocationExtender.Create(externalLocation)
            End If

            Throw Exceptions.ThrowEFail()
        End Function

        Public Overrides Function GetTypeExtenderNames() As String()
            Return {ExtenderNames.VBGenericExtender}
        End Function

        Public Overrides Function GetTypeExtender(name As String, codeType As AbstractCodeType) As Object
            If codeType Is Nothing Then
                Throw Exceptions.ThrowEUnexpected()
            End If

            If StringComparer.OrdinalIgnoreCase.Equals(name, ExtenderNames.VBGenericExtender) Then
                Return GenericExtender.Create(codeType)
            End If

            Throw Exceptions.ThrowEFail()
        End Function

        Protected Overrides Function AddBlankLineToMethodBody(node As SyntaxNode, newNode As SyntaxNode) As Boolean
            Return TypeOf node Is SyntaxNode AndAlso
                   node.IsKind(SyntaxKind.SubStatement, SyntaxKind.FunctionStatement) AndAlso
                   TypeOf newNode Is SyntaxNode AndAlso
                   newNode.IsKind(SyntaxKind.SubBlock, SyntaxKind.FunctionBlock)
        End Function

        Public Overrides Function IsValidBaseType(node As SyntaxNode, typeSymbol As ITypeSymbol) As Boolean
            If node.IsKind(SyntaxKind.ClassBlock) Then
                Return typeSymbol.TypeKind = TypeKind.Class
            ElseIf node.IsKind(SyntaxKind.InterfaceBlock) Then
                Return typeSymbol.TypeKind = TypeKind.Interface
            End If

            Return False
        End Function

        Public Overrides Function AddBase(node As SyntaxNode, typeSymbol As ITypeSymbol, semanticModel As SemanticModel, position As Integer?) As SyntaxNode
            If Not node.IsKind(SyntaxKind.ClassBlock, SyntaxKind.InterfaceBlock) Then
                Throw Exceptions.ThrowENotImpl()
            End If

            Dim typeBlock = DirectCast(node, TypeBlockSyntax)
            Dim baseCount = typeBlock.Inherits.Count

            If typeBlock.IsKind(SyntaxKind.ClassBlock) AndAlso baseCount > 0 Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim typeBlockPosition = typeBlock.SpanStart

            Dim inheritsStatement =
                SyntaxFactory.InheritsStatement(
                    SyntaxFactory.ParseTypeName(
                        typeSymbol.ToMinimalDisplayString(semanticModel, typeBlockPosition)))

            inheritsStatement = inheritsStatement.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)

            Dim inheritsStatements = typeBlock.Inherits.Insert(0, inheritsStatement)

            Return typeBlock.WithInherits(inheritsStatements)
        End Function

        Public Overrides Function RemoveBase(node As SyntaxNode, typeSymbol As ITypeSymbol, semanticModel As SemanticModel) As SyntaxNode
            If Not node.IsKind(SyntaxKind.ClassBlock, SyntaxKind.InterfaceBlock) Then
                Throw Exceptions.ThrowENotImpl()
            End If

            Dim typeBlock = DirectCast(node, TypeBlockSyntax)
            If typeBlock.Inherits.Count = 0 Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim inheritsStatements = typeBlock.Inherits
            Dim foundType = False

            For Each inheritsStatement In inheritsStatements
                For Each inheritsType In inheritsStatement.Types
                    Dim typeInfo = semanticModel.GetTypeInfo(inheritsType, CancellationToken.None)
                    If typeInfo.Type IsNot Nothing AndAlso
                       typeInfo.Type.Equals(typeSymbol) Then

                        foundType = True

                        If inheritsStatement.Types.Count = 1 Then
                            inheritsStatements = inheritsStatements.Remove(inheritsStatement)
                        Else
                            Dim newInheritsStatement = inheritsStatement.RemoveNode(inheritsType, SyntaxRemoveOptions.KeepEndOfLine)
                            inheritsStatements = inheritsStatements.Replace(inheritsStatement, newInheritsStatement)
                        End If

                        Exit For
                    End If
                Next
            Next

            Return typeBlock.WithInherits(inheritsStatements)
        End Function

        Public Overrides Function IsValidInterfaceType(node As SyntaxNode, typeSymbol As ITypeSymbol) As Boolean
            If node.IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) Then
                Return typeSymbol.TypeKind = TypeKind.Interface
            End If

            Return False
        End Function

        Public Overrides Function AddImplementedInterface(node As SyntaxNode, typeSymbol As ITypeSymbol, semanticModel As SemanticModel, position As Integer?) As SyntaxNode
            If Not node.IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) Then
                Throw Exceptions.ThrowENotImpl()
            End If

            Dim typeBlock = DirectCast(node, TypeBlockSyntax)
            Dim baseCount = typeBlock.Implements.Count

            Dim insertionIndex As Integer
            If position IsNot Nothing Then
                insertionIndex = position.Value
                If insertionIndex > baseCount Then
                    Throw Exceptions.ThrowEInvalidArg()
                End If
            Else
                insertionIndex = baseCount
            End If

            Dim typeBlockPosition = typeBlock.SpanStart

            Dim implementsStatement =
                SyntaxFactory.ImplementsStatement(
                    SyntaxFactory.ParseTypeName(
                        typeSymbol.ToMinimalDisplayString(semanticModel, typeBlockPosition)))

            implementsStatement = implementsStatement.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)

            Dim implementsStatements = typeBlock.Implements.Insert(insertionIndex, implementsStatement)

            Return typeBlock.WithImplements(implementsStatements)
        End Function

        Public Overrides Function RemoveImplementedInterface(node As SyntaxNode, typeSymbol As ITypeSymbol, semanticModel As SemanticModel) As SyntaxNode
            If Not node.IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) Then
                Throw Exceptions.ThrowENotImpl()
            End If

            Dim typeBlock = DirectCast(node, TypeBlockSyntax)
            If typeBlock.Implements.Count = 0 Then
                Throw Exceptions.ThrowEFail()
            End If

            Dim implementsStatements = typeBlock.Implements
            Dim foundType = False

            For Each implementsStatement In implementsStatements
                For Each inheritsType In implementsStatement.Types
                    Dim typeInfo = semanticModel.GetTypeInfo(inheritsType, CancellationToken.None)
                    If typeInfo.Type IsNot Nothing AndAlso
                       typeInfo.Type.Equals(typeSymbol) Then

                        foundType = True

                        If implementsStatement.Types.Count = 1 Then
                            implementsStatements = implementsStatements.Remove(implementsStatement)
                        Else
                            Dim newImplementsStatement = implementsStatement.RemoveNode(inheritsType, SyntaxRemoveOptions.KeepEndOfLine)
                            implementsStatements = implementsStatements.Replace(implementsStatement, newImplementsStatement)
                        End If

                        Exit For
                    End If
                Next
            Next

            Return typeBlock.WithImplements(implementsStatements)
        End Function

        Public Overrides Sub AttachFormatTrackingToBuffer(buffer As ITextBuffer)
            _commitBufferManagerFactory.CreateForBuffer(buffer).AddReferencingView()
        End Sub

        Public Overrides Sub DetachFormatTrackingToBuffer(buffer As ITextBuffer)
            _commitBufferManagerFactory.CreateForBuffer(buffer).RemoveReferencingView()
        End Sub

        Public Overrides Sub EnsureBufferFormatted(buffer As ITextBuffer)
            _commitBufferManagerFactory.CreateForBuffer(buffer).CommitDirty(isExplicitFormat:=False, cancellationToken:=Nothing)
        End Sub

    End Class
End Namespace
