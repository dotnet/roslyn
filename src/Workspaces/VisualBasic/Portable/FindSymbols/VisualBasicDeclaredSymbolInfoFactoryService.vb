' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FindSymbols
    <ExportLanguageService(GetType(IDeclaredSymbolInfoFactoryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDeclaredSymbolInfoFactoryService
        Inherits AbstractDeclaredSymbolInfoFactoryService(Of
            CompilationUnitSyntax,
            ImportsStatementSyntax,
            NamespaceBlockSyntax,
            TypeBlockSyntax,
            EnumBlockSyntax,
            DeclarationStatementSyntax,
            DeclarationStatementSyntax,
            StatementSyntax,
            NameSyntax,
            QualifiedNameSyntax,
            IdentifierNameSyntax)

        Private Const ExtensionName As String = "Extension"
        Private Const ExtensionAttributeName As String = "ExtensionAttribute"

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Private Shared Function GetInheritanceNames(stringTable As StringTable, typeBlock As TypeBlockSyntax) As ImmutableArray(Of String)
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

        Private Shared Function GetAliasMap(typeBlock As TypeBlockSyntax) As Dictionary(Of String, String)
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

        Private Shared Sub AddInheritanceNames(
                builder As ArrayBuilder(Of String),
                types As SeparatedSyntaxList(Of TypeSyntax),
                aliasMap As Dictionary(Of String, String))

            For Each typeSyntax In types
                AddInheritanceName(builder, typeSyntax, aliasMap)
            Next
        End Sub

        Private Shared Sub AddInheritanceName(
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

        Private Shared Function GetTypeName(typeSyntax As TypeSyntax) As String
            If TypeOf typeSyntax Is SimpleNameSyntax Then
                Return GetSimpleName(DirectCast(typeSyntax, SimpleNameSyntax))
            ElseIf TypeOf typeSyntax Is QualifiedNameSyntax Then
                Return GetSimpleName(DirectCast(typeSyntax, QualifiedNameSyntax).Right)
            End If

            Return Nothing
        End Function

        Private Shared Function GetSimpleName(simpleName As SimpleNameSyntax) As String
            Return simpleName.Identifier.ValueText
        End Function

        Protected Overrides Function GetContainerDisplayName(node As StatementSyntax) As String
            Return VisualBasicSyntaxFacts.Instance.GetDisplayName(node, DisplayNameOptions.IncludeTypeParameters)
        End Function

        Protected Overrides Function GetFullyQualifiedContainerName(node As StatementSyntax, rootNamespace As String) As String
            Return VisualBasicSyntaxFacts.Instance.GetDisplayName(node, DisplayNameOptions.IncludeNamespaces, rootNamespace)
        End Function

        Protected Overrides Sub AddLocalFunctionInfos(node As StatementSyntax, stringTable As StringTable, declaredSymbolInfos As ArrayBuilder(Of DeclaredSymbolInfo), containerDisplayName As String, fullyQualifiedContainerName As String, cancellationToken As CancellationToken)
            ' VB doesn't have local functions.
        End Sub

        Protected Overrides Sub AddSynthesizedDeclaredSymbolInfos(container As SyntaxNode, memberDeclaration As StatementSyntax, stringTable As StringTable, declaredSymbolInfos As ArrayBuilder(Of DeclaredSymbolInfo), containerDisplayName As String, fullyQualifiedContainerName As String, cancellationToken As CancellationToken)
            ' Nothing to do in VB.
        End Sub

        Protected Overrides Function GetTypeDeclarationInfo(
                container As SyntaxNode,
                typeDeclaration As TypeBlockSyntax,
                stringTable As StringTable,
                containerDisplayName As String,
                fullyQualifiedContainerName As String) As DeclaredSymbolInfo?

            Dim blockStatement = typeDeclaration.BlockStatement

            ' If this Is a part of partial type that only contains nested types, then we don't make an info type for it.
            ' That's because we effectively think of this as just being a virtual container just to hold the nested
            ' types, And Not something someone would want to explicitly navigate to itself.  Similar to how we think of
            ' namespaces.
            If blockStatement.Modifiers.Any(SyntaxKind.PartialKeyword) AndAlso
               typeDeclaration.Members.Any() AndAlso
               typeDeclaration.Members.All(Function(m) TypeOf m Is TypeBlockSyntax) Then

                Return Nothing
            End If

            Return DeclaredSymbolInfo.Create(
                stringTable,
                blockStatement.Identifier.ValueText,
                GetTypeParameterSuffix(blockStatement.TypeParameterList),
                containerDisplayName,
                fullyQualifiedContainerName,
                blockStatement.Modifiers.Any(SyntaxKind.PartialKeyword),
                blockStatement.AttributeLists.Any(),
                GetDeclaredSymbolInfoKind(typeDeclaration),
                GetAccessibility(container, typeDeclaration, blockStatement.Modifiers),
                blockStatement.Identifier.Span,
                GetInheritanceNames(stringTable, typeDeclaration),
                IsNestedType(typeDeclaration),
                typeParameterCount:=If(blockStatement.TypeParameterList?.Parameters.Count, 0))
        End Function

        Protected Overrides Function GetEnumDeclarationInfo(
                container As SyntaxNode,
                enumDeclaration As EnumBlockSyntax,
                stringTable As StringTable,
                containerDisplayName As String,
                fullyQualifiedContainerName As String) As DeclaredSymbolInfo

            Dim enumStatement = enumDeclaration.EnumStatement

            Return DeclaredSymbolInfo.Create(
                stringTable,
                enumStatement.Identifier.ValueText, Nothing,
                containerDisplayName,
                fullyQualifiedContainerName,
                enumStatement.Modifiers.Any(SyntaxKind.PartialKeyword),
                enumStatement.AttributeLists.Any(),
                DeclaredSymbolInfoKind.Enum,
                GetAccessibility(container, enumDeclaration, enumStatement.Modifiers),
                enumStatement.Identifier.Span,
                ImmutableArray(Of String).Empty,
                IsNestedType(enumDeclaration))
        End Function

        Protected Overrides Sub AddMemberDeclarationInfos(
                container As SyntaxNode,
                node As StatementSyntax,
                stringTable As StringTable,
                declaredSymbolInfos As ArrayBuilder(Of DeclaredSymbolInfo),
                containerDisplayName As String,
                fullyQualifiedContainerName As String)

            If node.Kind() = SyntaxKind.PropertyBlock Then
                node = DirectCast(node, PropertyBlockSyntax).PropertyStatement
            ElseIf node.Kind() = SyntaxKind.EventBlock Then
                node = DirectCast(node, EventBlockSyntax).EventStatement
            ElseIf TypeOf node Is MethodBlockBaseSyntax Then
                node = DirectCast(node, MethodBlockBaseSyntax).BlockStatement
            End If

            Dim kind = node.Kind()
            Select Case kind
                Case SyntaxKind.SubNewStatement
                    Dim constructor = DirectCast(node, SubNewStatementSyntax)
                    Dim typeBlock = TryCast(container, TypeBlockSyntax)
                    If typeBlock IsNot Nothing Then
                        declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                            stringTable,
                            typeBlock.BlockStatement.Identifier.ValueText,
                            GetConstructorSuffix(constructor),
                            containerDisplayName,
                            fullyQualifiedContainerName,
                            constructor.Modifiers.Any(SyntaxKind.PartialKeyword),
                            constructor.AttributeLists.Any(),
                            DeclaredSymbolInfoKind.Constructor,
                            GetAccessibility(container, constructor, constructor.Modifiers),
                            constructor.NewKeyword.Span,
                            ImmutableArray(Of String).Empty,
                            parameterCount:=If(constructor.ParameterList?.Parameters.Count, 0)))

                        Return
                    End If
                Case SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                    Dim delegateDecl = DirectCast(node, DelegateStatementSyntax)
                    declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                        stringTable,
                        delegateDecl.Identifier.ValueText,
                        GetTypeParameterSuffix(delegateDecl.TypeParameterList),
                        containerDisplayName,
                        fullyQualifiedContainerName,
                        delegateDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                        delegateDecl.AttributeLists.Any(),
                        DeclaredSymbolInfoKind.Delegate,
                        GetAccessibility(container, delegateDecl, delegateDecl.Modifiers),
                        delegateDecl.Identifier.Span,
                        ImmutableArray(Of String).Empty))
                    Return
                Case SyntaxKind.EnumMemberDeclaration
                    Dim enumMember = DirectCast(node, EnumMemberDeclarationSyntax)
                    declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                        stringTable,
                        enumMember.Identifier.ValueText, Nothing,
                        containerDisplayName,
                        fullyQualifiedContainerName,
                        isPartial:=False,
                        enumMember.AttributeLists.Any(),
                        DeclaredSymbolInfoKind.EnumMember,
                        Accessibility.Public,
                        enumMember.Identifier.Span,
                        ImmutableArray(Of String).Empty))
                    Return
                Case SyntaxKind.EventStatement
                    Dim eventDecl = DirectCast(node, EventStatementSyntax)
                    declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                        stringTable,
                        eventDecl.Identifier.ValueText, Nothing,
                        containerDisplayName,
                        fullyQualifiedContainerName,
                        eventDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                        eventDecl.AttributeLists.Any(),
                        DeclaredSymbolInfoKind.Event,
                        GetAccessibility(container, eventDecl, eventDecl.Modifiers),
                        eventDecl.Identifier.Span,
                        ImmutableArray(Of String).Empty))
                    Return
                Case SyntaxKind.FunctionStatement, SyntaxKind.SubStatement
                    Dim funcDecl = DirectCast(node, MethodStatementSyntax)
                    Dim isExtension = IsExtensionMethod(funcDecl)
                    declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                        stringTable,
                        funcDecl.Identifier.ValueText,
                        GetMethodSuffix(funcDecl),
                        containerDisplayName,
                        fullyQualifiedContainerName,
                        funcDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                        funcDecl.AttributeLists.Any(),
                        If(isExtension, DeclaredSymbolInfoKind.ExtensionMethod, DeclaredSymbolInfoKind.Method),
                        GetAccessibility(container, funcDecl, funcDecl.Modifiers),
                        funcDecl.Identifier.Span,
                        ImmutableArray(Of String).Empty,
                        parameterCount:=If(funcDecl.ParameterList?.Parameters.Count, 0),
                        typeParameterCount:=If(funcDecl.TypeParameterList?.Parameters.Count, 0)))

                    Return
                Case SyntaxKind.PropertyStatement
                    Dim propertyDecl = DirectCast(node, PropertyStatementSyntax)
                    declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                        stringTable,
                        propertyDecl.Identifier.ValueText,
                        GetPropertySuffix(propertyDecl),
                        containerDisplayName,
                        fullyQualifiedContainerName,
                        propertyDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                        propertyDecl.AttributeLists.Any(),
                        DeclaredSymbolInfoKind.Property,
                        GetAccessibility(container, propertyDecl, propertyDecl.Modifiers),
                        propertyDecl.Identifier.Span,
                        ImmutableArray(Of String).Empty))
                    Return
                Case SyntaxKind.FieldDeclaration
                    Dim fieldDecl = DirectCast(node, FieldDeclarationSyntax)
                    For Each variableDeclarator In fieldDecl.Declarators
                        For Each modifiedIdentifier In variableDeclarator.Names
                            declaredSymbolInfos.Add(DeclaredSymbolInfo.Create(
                                stringTable,
                                modifiedIdentifier.Identifier.ValueText, Nothing,
                                containerDisplayName,
                                fullyQualifiedContainerName,
                                fieldDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                                fieldDecl.AttributeLists.Any(),
                                If(fieldDecl.Modifiers.Any(Function(m) m.Kind() = SyntaxKind.ConstKeyword),
                                    DeclaredSymbolInfoKind.Constant,
                                    DeclaredSymbolInfoKind.Field),
                                GetAccessibility(container, fieldDecl, fieldDecl.Modifiers),
                                modifiedIdentifier.Identifier.Span,
                                ImmutableArray(Of String).Empty))
                        Next
                    Next
            End Select
        End Sub

        Protected Overrides Function GetChildren(node As CompilationUnitSyntax) As SyntaxList(Of StatementSyntax)
            Return node.Members
        End Function

        Protected Overrides Function GetChildren(node As NamespaceBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return node.Members
        End Function

        Protected Overrides Function GetChildren(node As TypeBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return node.Members
        End Function

        Protected Overrides Function GetChildren(node As EnumBlockSyntax) As IEnumerable(Of StatementSyntax)
            Return node.Members
        End Function

        Protected Overrides Function GetUsingAliases(node As CompilationUnitSyntax) As SyntaxList(Of ImportsStatementSyntax)
            Return node.Imports
        End Function

        Protected Overrides Function GetUsingAliases(node As NamespaceBlockSyntax) As SyntaxList(Of ImportsStatementSyntax)
            Return Nothing
        End Function

        Protected Overrides Function GetName(node As NamespaceBlockSyntax) As NameSyntax
            Return node.NamespaceStatement.Name
        End Function

        Protected Overrides Function GetLeft(node As QualifiedNameSyntax) As NameSyntax
            Return node.Left
        End Function

        Protected Overrides Function GetRight(node As QualifiedNameSyntax) As NameSyntax
            Return node.Right
        End Function

        Protected Overrides Function GetIdentifier(node As IdentifierNameSyntax) As SyntaxToken
            Return node.Identifier
        End Function

        Private Shared Function IsExtensionMethod(node As MethodStatementSyntax) As Boolean
            Dim parameterCount = node.ParameterList?.Parameters.Count

            ' Extension method must have at least one parameter and declared inside a module
            If Not parameterCount.HasValue OrElse parameterCount.Value = 0 OrElse TypeOf node.Parent?.Parent IsNot ModuleBlockSyntax Then
                Return False
            End If

            For Each attributeList In node.AttributeLists
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

        Private Shared Function IsNestedType(node As DeclarationStatementSyntax) As Boolean
            Return TypeOf node.Parent Is TypeBlockSyntax
        End Function

        Private Shared Function GetMethodSuffix(method As MethodStatementSyntax) As String
            Return GetTypeParameterSuffix(method.TypeParameterList) & GetSuffix(method.ParameterList)
        End Function

        Private Shared Function GetConstructorSuffix(method As SubNewStatementSyntax) As String
            Return ".New" & GetSuffix(method.ParameterList)
        End Function

        Private Shared Function GetPropertySuffix([property] As PropertyStatementSyntax) As String
            If [property].ParameterList Is Nothing Then
                Return Nothing
            End If

            Return GetSuffix([property].ParameterList)
        End Function

        Private Shared Function GetTypeParameterSuffix(typeParameterList As TypeParameterListSyntax) As String
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
        Private Shared Function GetSuffix(parameterList As ParameterListSyntax) As String
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

        Private Shared Sub AppendParameters(parameters As SeparatedSyntaxList(Of ParameterSyntax), builder As StringBuilder)
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
                    builder.Append(parameter.AsClause.Type.ConvertToSingleLine().ToString())
                End If

                First = False
            Next
        End Sub

        Protected Overrides Function GetExtensionReceiverTypeName(node As DeclarationStatementSyntax) As String
            node = If(TryCast(node, MethodBlockBaseSyntax)?.BlockStatement, node)

            Dim funcDecl = DirectCast(node, MethodStatementSyntax)
            Debug.Assert(IsExtensionMethod(funcDecl))

            Dim typeParameterNames = funcDecl.TypeParameterList?.Parameters.SelectAsArray(Function(p) p.Identifier.Text)
            Dim targetTypeName As String = Nothing
            Dim isArray As Boolean = False

            TryGetSimpleTypeNameWorker(funcDecl.ParameterList.Parameters(0).AsClause?.Type, typeParameterNames, targetTypeName, isArray)
            Return CreateReceiverTypeString(targetTypeName, isArray)
        End Function

        Protected Overrides Function GetExtensionReceiverTypeName(node As TypeBlockSyntax) As String
            ' VB does not have extension blocks
            Return Nothing
        End Function

        Protected Overrides Function TryGetAliasesFromUsingDirective(importStatement As ImportsStatementSyntax, ByRef aliases As ImmutableArray(Of (aliasName As String, name As String))) As Boolean
            Dim builder = ArrayBuilder(Of (String, String)).GetInstance()

            If importStatement IsNot Nothing Then
                For Each importsClause In importStatement.ImportsClauses

                    If importsClause.Kind = SyntaxKind.SimpleImportsClause Then
                        Dim simpleImportsClause = DirectCast(importsClause, SimpleImportsClauseSyntax)
                        Dim aliasName, name As String

#Disable Warning BC42030 ' Variable is passed by reference before it has been assigned a value
                        If simpleImportsClause.Alias IsNot Nothing AndAlso
                            TryGetSimpleTypeNameWorker(simpleImportsClause.Alias, Nothing, aliasName, Nothing) AndAlso
                            TryGetSimpleTypeNameWorker(simpleImportsClause, Nothing, name, Nothing) Then
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

        Private Shared Function TryGetSimpleTypeNameWorker(node As SyntaxNode, typeParameterNames As ImmutableArray(Of String)?, ByRef simpleTypeName As String, ByRef isArray As Boolean) As Boolean

            isArray = False

            If TypeOf node Is IdentifierNameSyntax Then
                Dim identifierName = DirectCast(node, IdentifierNameSyntax)
                Dim text = identifierName.Identifier.Text
                simpleTypeName = If(typeParameterNames?.Contains(text), Nothing, text)
                Return simpleTypeName IsNot Nothing

            ElseIf TypeOf node Is ArrayTypeSyntax Then
                isArray = True
                Dim arrayType = DirectCast(node, ArrayTypeSyntax)
                Return TryGetSimpleTypeNameWorker(arrayType.ElementType, typeParameterNames, simpleTypeName, Nothing)

            ElseIf TypeOf node Is GenericNameSyntax Then
                Dim genericName = DirectCast(node, GenericNameSyntax)
                Dim name = genericName.Identifier.Text
                Dim arity = genericName.Arity
                simpleTypeName = If(arity = 0, name, name + ArityUtilities.GetMetadataAritySuffix(arity))
                Return True

            ElseIf TypeOf node Is QualifiedNameSyntax Then
                ' For an identifier to the right of a '.', it can't be a type parameter,
                ' so we don't need to check for it further.
                Dim qualifiedName = DirectCast(node, QualifiedNameSyntax)
                Return TryGetSimpleTypeNameWorker(qualifiedName.Right, Nothing, simpleTypeName, Nothing)

            ElseIf TypeOf node Is NullableTypeSyntax Then
                Return TryGetSimpleTypeNameWorker(DirectCast(node, NullableTypeSyntax).ElementType, typeParameterNames, simpleTypeName, isArray)

            ElseIf TypeOf node Is PredefinedTypeSyntax Then
                simpleTypeName = GetSpecialTypeName(DirectCast(node, PredefinedTypeSyntax))
                Return simpleTypeName IsNot Nothing

            ElseIf TypeOf node Is TupleTypeSyntax Then
                Dim tupleArity = DirectCast(node, TupleTypeSyntax).Elements.Count
                simpleTypeName = CreateValueTupleTypeString(tupleArity)
                Return True
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

        Protected Overrides Function GetRootNamespace(compilationOptions As CompilationOptions) As String
            Return DirectCast(compilationOptions, VisualBasicCompilationOptions).RootNamespace
        End Function
    End Class
End Namespace
