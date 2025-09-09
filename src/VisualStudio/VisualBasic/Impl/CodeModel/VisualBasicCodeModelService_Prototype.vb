' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Partial Friend Class VisualBasicCodeModelService

        Private Shared ReadOnly s_prototypeFullNameFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.ExpandNullable Or SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Private Shared ReadOnly s_prototypeTypeNameFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.ExpandNullable Or SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Public Overrides Function GetPrototype(node As SyntaxNode, symbol As ISymbol, flags As PrototypeFlags) As String
            Debug.Assert(symbol IsNot Nothing)

            Select Case symbol.Kind
                Case SymbolKind.Field
                    Return GetVariablePrototype(DirectCast(symbol, IFieldSymbol), flags)
                Case SymbolKind.Property
                    Dim propertySymbol = DirectCast(symbol, IPropertySymbol)
                    Return GetFunctionPrototype(propertySymbol, propertySymbol.Parameters, flags)
                Case SymbolKind.Method
                    Dim methodSymbol = DirectCast(symbol, IMethodSymbol)
                    Return GetFunctionPrototype(methodSymbol, methodSymbol.Parameters, flags)
                Case SymbolKind.NamedType
                    Dim namedTypeSymbol = DirectCast(symbol, INamedTypeSymbol)
                    If namedTypeSymbol.TypeKind = TypeKind.Delegate Then
                        Return GetFunctionPrototype(namedTypeSymbol, namedTypeSymbol.DelegateInvokeMethod.Parameters, flags)
                    End If
                Case SymbolKind.Event
                    Dim eventSymbol = DirectCast(symbol, IEventSymbol)
                    Dim eventType = TryCast(eventSymbol.Type, INamedTypeSymbol)
                    Dim parameters = If(eventType IsNot Nothing AndAlso eventType.DelegateInvokeMethod IsNot Nothing,
                                        eventType.DelegateInvokeMethod.Parameters,
                                        ImmutableArray.Create(Of IParameterSymbol)())

                    Return GetEventPrototype(eventSymbol, parameters, flags)
            End Select

            Debug.Fail(String.Format("Invalid symbol kind: {0}", symbol.Kind))
            Throw Exceptions.ThrowEUnexpected()
        End Function

        Private Shared Function GetEventPrototype(symbol As IEventSymbol, parameters As ImmutableArray(Of IParameterSymbol), flags As PrototypeFlags) As String
            If Not AreValidFunctionPrototypeFlags(flags) Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            If flags = PrototypeFlags.Signature Then
                Return symbol.GetDocumentationCommentId()
            End If

            Dim builder = New StringBuilder()

            AppendName(builder, symbol, flags)
            AppendParameters(builder, parameters, flags)
            AppendType(builder, symbol.Type, flags)

            Return builder.ToString()
        End Function

        Private Shared Function GetFunctionPrototype(symbol As ISymbol, parameters As ImmutableArray(Of IParameterSymbol), flags As PrototypeFlags) As String
            If Not AreValidFunctionPrototypeFlags(flags) Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            If flags = PrototypeFlags.Signature Then
                Return symbol.GetDocumentationCommentId()
            End If

            Dim builder = New StringBuilder()

            AppendName(builder, symbol, flags)
            AppendParameters(builder, parameters, flags)

            If symbol.Kind = SymbolKind.Method Then
                Dim methodSymbol = DirectCast(symbol, IMethodSymbol)
                If methodSymbol.MethodKind = MethodKind.Ordinary AndAlso
                   methodSymbol.ReturnType IsNot Nothing AndAlso
                   Not methodSymbol.ReturnType.SpecialType = SpecialType.System_Void Then

                    AppendType(builder, methodSymbol.ReturnType, flags)
                End If
            End If

            Return builder.ToString()
        End Function

        Private Shared Function GetVariablePrototype(symbol As IFieldSymbol, flags As PrototypeFlags) As String
            If Not AreValidVariablePrototypeFlags(flags) Then
                Throw Exceptions.ThrowEInvalidArg()
            End If

            If flags = PrototypeFlags.Signature Then
                Return symbol.GetDocumentationCommentId()
            End If

            Dim builder = New StringBuilder()

            AppendAccessibility(builder, symbol.DeclaredAccessibility)
            builder.Append(" "c)

            AppendName(builder, symbol, flags)
            AppendType(builder, symbol.Type, flags)

            If (flags And PrototypeFlags.Initializer) <> 0 Then
                Dim modifiedIdentifier = TryCast(symbol.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax(), ModifiedIdentifierSyntax)
                If modifiedIdentifier IsNot Nothing Then
                    Dim variableDeclarator = TryCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
                    If variableDeclarator IsNot Nothing AndAlso
                       variableDeclarator.Initializer IsNot Nothing AndAlso
                       variableDeclarator.Initializer.Value IsNot Nothing AndAlso
                       Not variableDeclarator.Initializer.Value.IsMissing Then

                        builder.Append(" = ")
                        builder.Append(variableDeclarator.Initializer.Value.ToString())
                    End If
                End If
            End If

            Return builder.ToString()
        End Function

        Private Shared Sub AppendAccessibility(builder As StringBuilder, accessibility As Accessibility)
            Select accessibility
                Case Accessibility.Private
                    builder.Append("Private")
                Case Accessibility.Protected
                    builder.Append("Protected")
                Case Accessibility.Friend
                    builder.Append("Friend")
                Case Accessibility.ProtectedOrFriend
                    builder.Append("Protected Friend")
                Case Accessibility.ProtectedAndFriend
                    builder.Append("Private Protected")
                Case Accessibility.Public
                    builder.Append("Public")
            End Select
        End Sub

        Private Shared Sub AppendName(builder As StringBuilder, symbol As ISymbol, flags As PrototypeFlags)
            Select Case flags And PrototypeFlags.NameMask
                Case PrototypeFlags.FullName
                    builder.Append(symbol.ToDisplayString(s_prototypeFullNameFormat))
                Case PrototypeFlags.TypeName
                    builder.Append(symbol.ToDisplayString(s_prototypeTypeNameFormat))
                Case PrototypeFlags.BaseName
                    builder.Append(symbol.Name)
            End Select
        End Sub

        Private Shared Sub AppendParameters(builder As StringBuilder, parameters As ImmutableArray(Of IParameterSymbol), flags As PrototypeFlags)
            builder.Append("("c)

            If (flags And PrototypeFlags.ParametersMask) <> 0 Then
                Dim first = True
                For Each parameter In parameters
                    If Not first Then
                        builder.Append(", ")
                    End If

                    If (flags And PrototypeFlags.ParameterNames) <> 0 Then
                        builder.Append(parameter.Name)
                        builder.Append(" "c)

                        If (flags And PrototypeFlags.ParameterTypes) <> 0 Then
                            builder.Append("As ")
                        End If
                    End If

                    If (flags And PrototypeFlags.ParameterTypes) <> 0 Then
                        builder.Append(parameter.Type.ToDisplayString(s_prototypeFullNameFormat))
                    End If

                    If (flags And PrototypeFlags.ParameterDefaultValues) <> 0 Then
                        Dim parameterNode = TryCast(parameter.DeclaringSyntaxReferences(0).GetSyntax(), ParameterSyntax)
                        If parameterNode IsNot Nothing AndAlso
                           parameterNode.Default IsNot Nothing AndAlso
                           parameterNode.Default.Value IsNot Nothing AndAlso
                           Not parameterNode.Default.Value.IsMissing Then

                            builder.Append(" = ")
                            builder.Append(parameterNode.Default.Value.ToString())
                        End If
                    End If
                Next
            End If

            builder.Append(")"c)
        End Sub

        Private Shared Sub AppendType(builder As StringBuilder, type As ITypeSymbol, flags As PrototypeFlags)
            If (flags And PrototypeFlags.Type) <> 0 Then
                builder.Append(" As ")
                builder.Append(type.ToDisplayString(s_prototypeFullNameFormat))
            End If
        End Sub

        Private Shared Function AreValidFunctionPrototypeFlags(flags As PrototypeFlags) As Boolean
            ' Unsupported flags for functions
            If (flags And PrototypeFlags.Initializer) <> 0 Then
                Return False
            End If

            Return AreValidPrototypeFlags(flags)
        End Function

        Private Shared Function AreValidVariablePrototypeFlags(flags As PrototypeFlags) As Boolean
            ' Unsupported flags for variables
            If (flags And PrototypeFlags.ParametersMask) <> 0 Then
                Return False
            End If

            Return AreValidPrototypeFlags(flags)
        End Function

        Private Shared Function AreValidPrototypeFlags(ByRef flags As PrototypeFlags) As Boolean
            ' Signature cannot be combined with anything else
            If (flags And PrototypeFlags.Signature) <> 0 AndAlso flags <> PrototypeFlags.Signature Then
                Return False
            End If

            ' Only one name flag can be specified
            Dim nameFlags = flags And PrototypeFlags.NameMask
            If nameFlags <> 0 Then
                If Not nameFlags = PrototypeFlags.FullName AndAlso
                   Not nameFlags = PrototypeFlags.TypeName AndAlso
                   Not nameFlags = PrototypeFlags.NoName Then

                    Return False
                End If
            End If

            Return True
        End Function

    End Class
End Namespace
