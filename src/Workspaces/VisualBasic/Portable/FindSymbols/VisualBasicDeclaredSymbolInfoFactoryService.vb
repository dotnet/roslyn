' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FindSymbols
    <ExportLanguageService(GetType(IDeclaredSymbolInfoFactoryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDeclaredSymbolInfoFactoryService
        Inherits AbstractDeclaredSymbolInfoFactoryService

        Private Const ExtensionName As String = "Extension"
        Private Const ExtensionAttributeName As String = "ExtensionAttribute"

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Private Function GetInheritanceNames(stringTable As StringTable, typeBlock As TypeBlockSyntax) As ImmutableArray(Of String)
            Dim builder = ArrayBuilder(Of String).GetInstance()

            Dim aliasMap = GetAliasMap(typeBlock)
            Try
                For Each inheritsStatement In typeBlock.Inherits
                    AddInheritanceNames(builder, inheritsStatement.Types, aliasMap)
                Next

                For Each implementsStatement In typeBlock.Implements
                    AddInheritanceNames(builder, implementsStatement.Types, aliasMap)
                Next

                Intern(stringTable, builder)
                Return builder.ToImmutableAndFree()
            Finally
                FreeAliasMap(aliasMap)
            End Try
        End Function

        Private Function GetAliasMap(typeBlock As TypeBlockSyntax) As Dictionary(Of String, String)
            Dim compilationUnit = typeBlock.SyntaxTree.GetCompilationUnitRoot()

            Dim aliasMap As Dictionary(Of String, String) = Nothing
            For Each import In compilationUnit.Imports
                For Each clause In import.ImportsClauses
                    If clause.IsKind(SyntaxKind.SimpleImportsClause) Then
                        Dim simpleImport = DirectCast(clause, SimpleImportsClauseSyntax)
                        If simpleImport.Alias IsNot Nothing Then
                            Dim mappedName = GetTypeName(simpleImport.Name)
                            If mappedName IsNot Nothing Then
                                aliasMap = If(aliasMap, AllocateAliasMap())
                                aliasMap(simpleImport.Alias.Identifier.ValueText) = mappedName
                            End If
                        End If
                    End If
                Next
            Next

            Return aliasMap
        End Function

        Private Sub AddInheritanceNames(
                builder As ArrayBuilder(Of String),
                types As SeparatedSyntaxList(Of TypeSyntax),
                aliasMap As Dictionary(Of String, String))

            For Each typeSyntax In types
                AddInheritanceName(builder, typeSyntax, aliasMap)
            Next
        End Sub

        Private Sub AddInheritanceName(
                builder As ArrayBuilder(Of String),
                typeSyntax As TypeSyntax,
                aliasMap As Dictionary(Of String, String))
            Dim name = GetTypeName(typeSyntax)
            If name IsNot Nothing Then
                builder.Add(name)

                Dim mappedName As String = Nothing
                If aliasMap?.TryGetValue(name, mappedName) = True Then
                    ' Looks Like this could be an alias.  Also include the name the alias points to
                    builder.Add(mappedName)
                End If
            End If
        End Sub

        Private Function GetTypeName(typeSyntax As TypeSyntax) As String
            If TypeOf typeSyntax Is SimpleNameSyntax Then
                Return GetSimpleName(DirectCast(typeSyntax, SimpleNameSyntax))
            ElseIf TypeOf typeSyntax Is QualifiedNameSyntax Then
                Return GetSimpleName(DirectCast(typeSyntax, QualifiedNameSyntax).Right)
            End If

            Return Nothing
        End Function

        Private Function GetSimpleName(simpleName As SimpleNameSyntax) As String
            Return simpleName.Identifier.ValueText
        End Function

        Private Function GetContainerDisplayName(node As SyntaxNode) As String
            Return VisualBasicSyntaxFactsService.Instance.GetDisplayName(node, DisplayNameOptions.IncludeTypeParameters)
        End Function

        Private Function GetFullyQualifiedContainerName(node As SyntaxNode, rootNamespace As String) As String
            Return VisualBasicSyntaxFactsService.Instance.GetDisplayName(node, DisplayNameOptions.IncludeNamespaces, rootNamespace)
        End Function

        Public Overrides Function TryGetDeclaredSymbolInfo(stringTable As StringTable, node As SyntaxNode, rootNamespace As String, ByRef declaredSymbolInfo As DeclaredSymbolInfo) As Boolean
            Select Case node.Kind()
                Case SyntaxKind.ClassBlock
                    Dim classDecl = CType(node, ClassBlockSyntax)
                    declaredSymbolInfo = New DeclaredSymbolInfo(
                        stringTable,
                        classDecl.ClassStatement.Identifier.ValueText,
                        GetTypeParameterSuffix(classDecl.ClassStatement.TypeParameterList),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent, rootNamespace),
                        DeclaredSymbolInfoKind.Class,
                        GetAccessibility(classDecl, classDecl.ClassStatement.Modifiers),
                        classDecl.ClassStatement.Identifier.Span,
                        GetInheritanceNames(stringTable, classDecl),
                        IsNestedType(classDecl))
                    Return True
                Case SyntaxKind.EnumBlock
                    Dim enumDecl = CType(node, EnumBlockSyntax)
                    declaredSymbolInfo = New DeclaredSymbolInfo(
                        stringTable,
                        enumDecl.EnumStatement.Identifier.ValueText, Nothing,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent, rootNamespace),
                        DeclaredSymbolInfoKind.Enum,
                        GetAccessibility(enumDecl, enumDecl.EnumStatement.Modifiers),
                        enumDecl.EnumStatement.Identifier.Span,
                        ImmutableArray(Of String).Empty,
                        IsNestedType(enumDecl))
                    Return True
                Case SyntaxKind.InterfaceBlock
                    Dim interfaceDecl = CType(node, InterfaceBlockSyntax)
                    declaredSymbolInfo = New DeclaredSymbolInfo(
                        stringTable,
                        interfaceDecl.InterfaceStatement.Identifier.ValueText,
                        GetTypeParameterSuffix(interfaceDecl.InterfaceStatement.TypeParameterList),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent, rootNamespace),
                        DeclaredSymbolInfoKind.Interface,
                        GetAccessibility(interfaceDecl, interfaceDecl.InterfaceStatement.Modifiers),
                        interfaceDecl.InterfaceStatement.Identifier.Span,
                        GetInheritanceNames(stringTable, interfaceDecl),
                        IsNestedType(interfaceDecl))
                    Return True
                Case SyntaxKind.ModuleBlock
                    Dim moduleDecl = CType(node, ModuleBlockSyntax)
                    declaredSymbolInfo = New DeclaredSymbolInfo(
                        stringTable,
                        moduleDecl.ModuleStatement.Identifier.ValueText,
                        GetTypeParameterSuffix(moduleDecl.ModuleStatement.TypeParameterList),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent, rootNamespace),
                        DeclaredSymbolInfoKind.Module,
                        GetAccessibility(moduleDecl, moduleDecl.ModuleStatement.Modifiers),
                        moduleDecl.ModuleStatement.Identifier.Span,
                        GetInheritanceNames(stringTable, moduleDecl),
                        IsNestedType(moduleDecl))
                    Return True
                Case SyntaxKind.StructureBlock
                    Dim structDecl = CType(node, StructureBlockSyntax)
                    declaredSymbolInfo = New DeclaredSymbolInfo(
                        stringTable,
                        structDecl.StructureStatement.Identifier.ValueText,
                        GetTypeParameterSuffix(structDecl.StructureStatement.TypeParameterList),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent, rootNamespace),
                        DeclaredSymbolInfoKind.Struct,
                        GetAccessibility(structDecl, structDecl.StructureStatement.Modifiers),
                        structDecl.StructureStatement.Identifier.Span,
                        GetInheritanceNames(stringTable, structDecl),
                        IsNestedType(structDecl))
                    Return True
                Case SyntaxKind.ConstructorBlock
                    Dim constructor = CType(node, ConstructorBlockSyntax)
                    Dim typeBlock = TryCast(constructor.Parent, TypeBlockSyntax)
                    If typeBlock IsNot Nothing Then
                        declaredSymbolInfo = New DeclaredSymbolInfo(
                            stringTable,
                            typeBlock.BlockStatement.Identifier.ValueText,
                            GetConstructorSuffix(constructor),
                            GetContainerDisplayName(node.Parent),
                            GetFullyQualifiedContainerName(node.Parent, rootNamespace),
                            DeclaredSymbolInfoKind.Constructor,
                            GetAccessibility(constructor, constructor.SubNewStatement.Modifiers),
                            constructor.SubNewStatement.NewKeyword.Span,
                            ImmutableArray(Of String).Empty,
                            parameterCount:=If(constructor.SubNewStatement.ParameterList?.Parameters.Count, 0))

                        Return True
                    End If
                Case SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                    Dim delegateDecl = CType(node, DelegateStatementSyntax)
                    declaredSymbolInfo = New DeclaredSymbolInfo(
                        stringTable,
                        delegateDecl.Identifier.ValueText,
                        GetTypeParameterSuffix(delegateDecl.TypeParameterList),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent, rootNamespace),
                        DeclaredSymbolInfoKind.Delegate,
                        GetAccessibility(delegateDecl, delegateDecl.Modifiers),
                        delegateDecl.Identifier.Span,
                        ImmutableArray(Of String).Empty)
                    Return True
                Case SyntaxKind.EnumMemberDeclaration
                    Dim enumMember = CType(node, EnumMemberDeclarationSyntax)
                    declaredSymbolInfo = New DeclaredSymbolInfo(
                        stringTable,
                        enumMember.Identifier.ValueText, Nothing,
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent, rootNamespace),
                        DeclaredSymbolInfoKind.EnumMember,
                        Accessibility.Public,
                        enumMember.Identifier.Span,
                        ImmutableArray(Of String).Empty)
                    Return True
                Case SyntaxKind.EventStatement
                    Dim eventDecl = CType(node, EventStatementSyntax)
                    Dim statementOrBlock = If(TypeOf node.Parent Is EventBlockSyntax, node.Parent, node)
                    Dim eventParent = statementOrBlock.Parent
                    declaredSymbolInfo = New DeclaredSymbolInfo(
                        stringTable,
                        eventDecl.Identifier.ValueText, Nothing,
                        GetContainerDisplayName(eventParent),
                        GetFullyQualifiedContainerName(eventParent, rootNamespace),
                        DeclaredSymbolInfoKind.Event,
                        GetAccessibility(statementOrBlock, eventDecl.Modifiers),
                        eventDecl.Identifier.Span,
                        ImmutableArray(Of String).Empty)
                    Return True
                Case SyntaxKind.FunctionBlock, SyntaxKind.SubBlock
                    Dim funcDecl = CType(node, MethodBlockSyntax)
                    declaredSymbolInfo = New DeclaredSymbolInfo(
                        stringTable,
                        funcDecl.SubOrFunctionStatement.Identifier.ValueText,
                        GetMethodSuffix(funcDecl),
                        GetContainerDisplayName(node.Parent),
                        GetFullyQualifiedContainerName(node.Parent, rootNamespace),
                        If(IsExtensionMethod(funcDecl), DeclaredSymbolInfoKind.ExtensionMethod, DeclaredSymbolInfoKind.Method),
                        GetAccessibility(node, funcDecl.SubOrFunctionStatement.Modifiers),
                        funcDecl.SubOrFunctionStatement.Identifier.Span,
                        ImmutableArray(Of String).Empty,
                        parameterCount:=If(funcDecl.SubOrFunctionStatement.ParameterList?.Parameters.Count, 0),
                        typeParameterCount:=If(funcDecl.SubOrFunctionStatement.TypeParameterList?.Parameters.Count, 0))
                    Return True
                Case SyntaxKind.ModifiedIdentifier
                    Dim modifiedIdentifier = CType(node, ModifiedIdentifierSyntax)
                    Dim variableDeclarator = TryCast(modifiedIdentifier.Parent, VariableDeclaratorSyntax)
                    Dim fieldDecl = TryCast(variableDeclarator?.Parent, FieldDeclarationSyntax)
                    If fieldDecl IsNot Nothing Then
                        Dim kind = If(fieldDecl.Modifiers.Any(Function(m) m.Kind() = SyntaxKind.ConstKeyword),
                            DeclaredSymbolInfoKind.Constant,
                            DeclaredSymbolInfoKind.Field)
                        declaredSymbolInfo = New DeclaredSymbolInfo(
                            stringTable,
                            modifiedIdentifier.Identifier.ValueText, Nothing,
                            GetContainerDisplayName(fieldDecl.Parent),
                            GetFullyQualifiedContainerName(fieldDecl.Parent, rootNamespace),
                            kind, GetAccessibility(fieldDecl, fieldDecl.Modifiers),
                            modifiedIdentifier.Identifier.Span,
                            ImmutableArray(Of String).Empty)
                        Return True
                    End If
                Case SyntaxKind.PropertyStatement
                    Dim propertyDecl = CType(node, PropertyStatementSyntax)
                    Dim statementOrBlock = If(TypeOf node.Parent Is PropertyBlockSyntax, node.Parent, node)
                    Dim propertyParent = statementOrBlock.Parent
                    declaredSymbolInfo = New DeclaredSymbolInfo(
                        stringTable,
                        propertyDecl.Identifier.ValueText, GetPropertySuffix(propertyDecl),
                        GetContainerDisplayName(propertyParent),
                        GetFullyQualifiedContainerName(propertyParent, rootNamespace),
                        DeclaredSymbolInfoKind.Property,
                        GetAccessibility(statementOrBlock, propertyDecl.Modifiers),
                        propertyDecl.Identifier.Span,
                        ImmutableArray(Of String).Empty)
                    Return True
            End Select

            declaredSymbolInfo = Nothing
            Return False
        End Function

        Private Function IsExtensionMethod(node As MethodBlockSyntax) As Boolean
            Dim parameterCount = node.SubOrFunctionStatement.ParameterList?.Parameters.Count

            ' Extension method must have at least one parameter and declared inside a module
            If Not parameterCount.HasValue OrElse parameterCount.Value = 0 OrElse TypeOf node.Parent IsNot ModuleBlockSyntax Then
                Return False
            End If

            For Each attributeList In node.BlockStatement.AttributeLists
                For Each attribute In attributeList.Attributes
                    ' ExtensionAttribute takes no argument.
                    If attribute.ArgumentList?.Arguments.Count > 0 Then
                        Continue For
                    End If

                    Dim name = attribute.Name.GetRightmostName()?.ToString()
                    If String.Equals(name, ExtensionName, StringComparison.OrdinalIgnoreCase) OrElse String.Equals(name, ExtensionAttributeName, StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If
                Next
            Next

            Return False
        End Function

        Private Function IsNestedType(node As DeclarationStatementSyntax) As Boolean
            Return TypeOf node.Parent Is TypeBlockSyntax
        End Function

        Private Function GetAccessibility(node As SyntaxNode, modifiers As SyntaxTokenList) As Accessibility
            Dim sawFriend = False

            For Each modifier In modifiers
                Select Case modifier.Kind()
                    Case SyntaxKind.PublicKeyword : Return Accessibility.Public
                    Case SyntaxKind.PrivateKeyword : Return Accessibility.Private
                    Case SyntaxKind.ProtectedKeyword : Return Accessibility.Protected
                    Case SyntaxKind.FriendKeyword
                        sawFriend = True
                        Continue For
                End Select
            Next

            If sawFriend Then
                Return Accessibility.Internal
            End If

            ' No accessibility modifiers
            Select Case node.Parent.Kind()
                Case SyntaxKind.ClassBlock
                    ' In a class, fields and shared-constructors are private by default,
                    ' everything Else Is Public
                    If node.Kind() = SyntaxKind.FieldDeclaration Then
                        Return Accessibility.Private
                    End If

                    If node.Kind() = SyntaxKind.ConstructorBlock AndAlso
                       DirectCast(node, ConstructorBlockSyntax).SubNewStatement.Modifiers.Any(SyntaxKind.SharedKeyword) Then
                        Return Accessibility.Private
                    End If

                    Return Accessibility.Public

                Case SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.ModuleBlock
                    ' Everything in a struct/interface/module is public
                    Return Accessibility.Public
            End Select

            ' Otherwise, it's internal
            Return Accessibility.Internal
        End Function

        Private Function GetMethodSuffix(method As MethodBlockSyntax) As String
            Return GetTypeParameterSuffix(method.SubOrFunctionStatement.TypeParameterList) &
                   GetSuffix(method.SubOrFunctionStatement.ParameterList)
        End Function

        Private Function GetConstructorSuffix(method As ConstructorBlockSyntax) As String
            Return ".New" & GetSuffix(method.SubNewStatement.ParameterList)
        End Function

        Private Function GetPropertySuffix([property] As PropertyStatementSyntax) As String
            If [property].ParameterList Is Nothing Then
                Return Nothing
            End If

            Return GetSuffix([property].ParameterList)
        End Function

        Private Function GetTypeParameterSuffix(typeParameterList As TypeParameterListSyntax) As String
            If typeParameterList Is Nothing Then
                Return Nothing
            End If

            Dim pooledBuilder = PooledStringBuilder.GetInstance()

            Dim builder = pooledBuilder.Builder
            builder.Append("(Of ")

            Dim First = True
            For Each parameter In typeParameterList.Parameters
                If Not First Then
                    builder.Append(", ")
                End If

                builder.Append(parameter.Identifier.Text)
                First = False
            Next

            builder.Append(")"c)

            Return pooledBuilder.ToStringAndFree()
        End Function

        ''' <summary>
        ''' Builds up the suffix to show for something with parameters in navigate-to.
        ''' While it would be nice to just use the compiler SymbolDisplay API for this,
        ''' it would be too expensive as it requires going back to Symbols (which requires
        ''' creating compilations, etc.) in a perf sensitive area.
        ''' 
        ''' So, instead, we just build a reasonable suffix using the pure syntax that a 
        ''' user provided.  That means that if they wrote "Method(System.Int32 i)" we'll 
        ''' show that as "Method(System.Int32)" Not "Method(Integer)".  Given that this Is
        ''' actually what the user wrote, And it saves us from ever having to go back to
        ''' symbols/compilations, this Is well worth it, even if it does mean we have to
        ''' create our own 'symbol display' logic here.
        ''' </summary>
        Private Function GetSuffix(parameterList As ParameterListSyntax) As String
            If parameterList Is Nothing OrElse parameterList.Parameters.Count = 0 Then
                Return "()"
            End If

            Dim pooledBuilder = PooledStringBuilder.GetInstance()

            Dim builder = pooledBuilder.Builder
            builder.Append("("c)
            If parameterList IsNot Nothing Then
                AppendParameters(parameterList.Parameters, builder)
            End If
            builder.Append(")"c)

            Return pooledBuilder.ToStringAndFree()
        End Function

        Private Sub AppendParameters(parameters As SeparatedSyntaxList(Of ParameterSyntax), builder As StringBuilder)
            Dim First = True
            For Each parameter In parameters
                If Not First Then
                    builder.Append(", ")
                End If

                For Each modifier In parameter.Modifiers
                    If modifier.Kind() <> SyntaxKind.ByValKeyword Then
                        builder.Append(modifier.Text)
                        builder.Append(" "c)
                    End If
                Next

                If parameter.AsClause?.Type IsNot Nothing Then
                    AppendTokens(parameter.AsClause.Type, builder)
                End If

                First = False
            Next
        End Sub

        Public Overrides Function TryGetTargetTypeName(node As SyntaxNode, ByRef targetTypeName As String) As Boolean
            Dim funcDecl = TryCast(node, MethodBlockSyntax)
            'TODO: don't call `IsExtensionMethod` again
            If funcDecl IsNot Nothing AndAlso IsExtensionMethod(funcDecl) Then

                Dim typeParameterNames = funcDecl.SubOrFunctionStatement.TypeParameterList?.Parameters.SelectAsArray(Function(p) p.Identifier.Text)
                TryGetSimpleTypeNameWorker(funcDecl.BlockStatement.ParameterList.Parameters(0).AsClause?.Type, typeParameterNames, targetTypeName)
                Return True
            End If

            targetTypeName = Nothing
            Return False
        End Function

        Public Overrides Function TryGetAliasesFromUsingDirective(node As SyntaxNode, ByRef aliases As ImmutableArray(Of (aliasName As String, name As String))) As Boolean

            Dim importStatement = TryCast(node, ImportsStatementSyntax)
            Dim builder = ArrayBuilder(Of (String, String)).GetInstance()

            If (importStatement IsNot Nothing) Then
                For Each importsClause In importStatement.ImportsClauses

                    If importsClause.Kind = SyntaxKind.SimpleImportsClause Then
                        Dim simpleImportsClause = DirectCast(importsClause, SimpleImportsClauseSyntax)
                        Dim aliasName, name As String

#Disable Warning BC42030 ' Variable is passed by reference before it has been assigned a value
                        If simpleImportsClause.Alias IsNot Nothing AndAlso
                            TryGetSimpleTypeNameWorker(simpleImportsClause.Alias, Nothing, aliasName) AndAlso
                            TryGetSimpleTypeNameWorker(simpleImportsClause, Nothing, name) Then
#Enable Warning BC42030 ' Variable is passed by reference before it has been assigned a value

                            builder.Add((aliasName, name))
                        End If
                    End If
                Next

                aliases = builder.ToImmutableAndFree()
                Return True
            End If

            aliases = Nothing
            Return False
        End Function

        Private Shared Function TryGetSimpleTypeNameWorker(node As SyntaxNode, typeParameterNames As ImmutableArray(Of String)?, ByRef simpleTypeName As String) As Boolean
            If TypeOf node Is IdentifierNameSyntax Then
                Dim identifierName = DirectCast(node, IdentifierNameSyntax)
                Dim text = identifierName.Identifier.Text
                simpleTypeName = If(typeParameterNames?.Contains(text), Nothing, text)
                Return simpleTypeName IsNot Nothing

            ElseIf TypeOf node Is GenericNameSyntax Then
                Dim genericName = DirectCast(node, GenericNameSyntax)
                Dim name = genericName.Identifier.Text
                Dim arity = genericName.Arity
                simpleTypeName = If(arity = 0, name, name + GetMetadataAritySuffix(arity))
                Return True

            ElseIf TypeOf node Is QualifiedNameSyntax Then
                ' For an identifier to the right of a '.', it can't be a type parameter,
                ' so we don't need to check for it further.
                Dim qualifiedName = DirectCast(node, QualifiedNameSyntax)
                Return TryGetSimpleTypeNameWorker(qualifiedName.Right, Nothing, simpleTypeName)

            ElseIf TypeOf node Is NullableTypeSyntax Then
                Return TryGetSimpleTypeNameWorker(DirectCast(node, NullableTypeSyntax).ElementType, typeParameterNames, simpleTypeName)

            ElseIf TypeOf node Is PredefinedTypeSyntax Then
                simpleTypeName = GetSpecialTypeName(DirectCast(node, PredefinedTypeSyntax))
                Return simpleTypeName IsNot Nothing
            End If

            simpleTypeName = Nothing
            Return False
        End Function

        Private Shared Function GetSpecialTypeName(predefinedTypeNode As PredefinedTypeSyntax) As String
            Select Case predefinedTypeNode.Keyword.Kind()
                Case SyntaxKind.BooleanKeyword
                    Return "Boolean"
                Case SyntaxKind.ByteKeyword
                    Return "Byte"
                Case SyntaxKind.CharKeyword
                    Return "Char"
                Case SyntaxKind.DateKeyword
                    Return "DateTime"
                Case SyntaxKind.DecimalKeyword
                    Return "Decimal"
                Case SyntaxKind.DoubleKeyword
                    Return "Double"
                Case SyntaxKind.IntegerKeyword
                    Return "Int32"
                Case SyntaxKind.LongKeyword
                    Return "Int64"
                Case SyntaxKind.ObjectKeyword
                    Return "Object"
                Case SyntaxKind.SByteKeyword
                    Return "SByte"
                Case SyntaxKind.ShortKeyword
                    Return "Int16"
                Case SyntaxKind.SingleKeyword
                    Return "Single"
                Case SyntaxKind.StringKeyword
                    Return "String"
                Case SyntaxKind.UIntegerKeyword
                    Return "UInt32"
                Case SyntaxKind.ULongKeyword
                    Return "UInt64"
                Case SyntaxKind.UShortKeyword
                    Return "UInt16"
                Case Else
                    Return Nothing
            End Select
        End Function

        Public Overrides Function GetRootNamespace(compilationOptions As CompilationOptions) As String
            Return DirectCast(compilationOptions, VisualBasicCompilationOptions).RootNamespace
        End Function
    End Class
End Namespace
