' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Progression
    <ExportLanguageService(GetType(IProgressionLanguageService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicProgressionLanguageService
        Implements IProgressionLanguageService

        Public Iterator Function GetTopLevelNodesFromDocument(root As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of SyntaxNode) Implements IProgressionLanguageService.GetTopLevelNodesFromDocument
            If cancellationToken.IsCancellationRequested Then Return
            Dim nodes = New Stack(Of SyntaxNode)()
            nodes.Push(root)

            While nodes.Count > 0

                Dim node = nodes.Pop()

                If cancellationToken.IsCancellationRequested Then Continue While

                Select Case node.Kind
                    Case SyntaxKind.ClassBlock,
                         SyntaxKind.DelegateFunctionStatement,
                         SyntaxKind.DelegateSubStatement,
                         SyntaxKind.EnumBlock,
                         SyntaxKind.ModuleBlock,
                         SyntaxKind.InterfaceBlock,
                         SyntaxKind.StructureBlock,
                         SyntaxKind.FieldDeclaration,
                         SyntaxKind.SubBlock,
                         SyntaxKind.FunctionBlock,
                         SyntaxKind.PropertyBlock
                        Yield node
                    Case Else
                        nodes.Push(node.ChildNodes)
                End Select
            End While
        End Function

        Private Shared ReadOnly s_descriptionFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeContainingType,
            parameterOptions:=SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeParamsRefOut Or SymbolDisplayParameterOptions.IncludeOptionalBrackets,
            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Public Function GetDescriptionForSymbol(symbol As ISymbol, includeContainingSymbol As Boolean) As String Implements IProgressionLanguageService.GetDescriptionForSymbol
            Return GetSymbolText(symbol, False, s_descriptionFormat)
        End Function

        Private Shared ReadOnly s_labelFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
            parameterOptions:=SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeParamsRefOut Or SymbolDisplayParameterOptions.IncludeOptionalBrackets,
            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Public Function GetLabelForSymbol(symbol As ISymbol, includeContainingSymbol As Boolean) As String Implements IProgressionLanguageService.GetLabelForSymbol
            Return GetSymbolText(symbol, includeContainingSymbol, s_labelFormat)
        End Function

        Private Function GetSymbolText(symbol As ISymbol, includeContainingSymbol As Boolean, displayFormat As SymbolDisplayFormat) As String
            If symbol.Kind = SymbolKind.Field AndAlso symbol.ContainingType.TypeKind = TypeKind.Enum Then
                displayFormat = displayFormat.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeType)
            End If

            Dim label As String = symbol.ToDisplayString(displayFormat)

            If includeContainingSymbol AndAlso symbol.ContainingSymbol IsNot Nothing Then
                label += " (" + symbol.ContainingSymbol.ToDisplayString(displayFormat) + ")"
            End If

            Return label
        End Function
    End Class
End Namespace
