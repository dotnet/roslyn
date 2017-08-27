﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Public Function GetTopLevelNodesFromDocument(root As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of SyntaxNode) Implements IProgressionLanguageService.GetTopLevelNodesFromDocument
            ' TODO: Implement this lazily like in C#?
            Dim nodes = New Stack(Of SyntaxNode)()

            Dim result = New List(Of SyntaxNode)

            nodes.Push(root)

            While nodes.Count > 0
                cancellationToken.ThrowIfCancellationRequested()

                Dim node = nodes.Pop()

                If node.Kind = SyntaxKind.ClassBlock OrElse
                    node.Kind = SyntaxKind.DelegateFunctionStatement OrElse
                    node.Kind = SyntaxKind.DelegateSubStatement OrElse
                    node.Kind = SyntaxKind.EnumBlock OrElse
                    node.Kind = SyntaxKind.ModuleBlock OrElse
                    node.Kind = SyntaxKind.InterfaceBlock OrElse
                    node.Kind = SyntaxKind.StructureBlock OrElse
                    node.Kind = SyntaxKind.FieldDeclaration OrElse
                    node.Kind = SyntaxKind.SubBlock OrElse
                    node.Kind = SyntaxKind.FunctionBlock OrElse
                    node.Kind = SyntaxKind.PropertyBlock Then
                    result.Add(node)
                Else
                    For Each child In node.ChildNodes()
                        nodes.Push(child)
                    Next
                End If
            End While

            Return result
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
