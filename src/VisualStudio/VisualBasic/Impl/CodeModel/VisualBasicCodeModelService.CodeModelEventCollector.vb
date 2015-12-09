' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' VB has a difference from C# in that CodeModelEvents are not fired for a
' CodeElement unless it's Children are accessed. This is intended to be a
' performance improvement by not firing as many CodeModelEvents. This
' suppression of events is not supported in Roslyn.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel
    Partial Friend Class VisualBasicCodeModelService

        Protected Overrides Function CreateCodeModelEventCollector() As AbstractCodeModelEventCollector
            Return New CodeModelEventCollector(Me)
        End Function

        Private Class CodeModelEventCollector
            Inherits AbstractCodeModelEventCollector

            Public Sub New(codeModelService As AbstractCodeModelService)
                MyBase.New(codeModelService)
            End Sub

            Private Sub CompareCompilationUnits(oldRoot As CompilationUnitSyntax, newRoot As CompilationUnitSyntax, eventQueue As CodeModelEventQueue)
                Dim parent As SyntaxNode = Nothing

                ' Options
                CompareChildren(
                    AddressOf CompareOptions,
                    oldRoot.Options.AsReadOnlyList(),
                    newRoot.Options.AsReadOnlyList(),
                    parent,
                    CodeModelEventType.Unknown,
                    eventQueue)

                ' Imports
                CompareChildren(
                    AddressOf CompareImportsClauses,
                    GetImportsClauses(oldRoot.Imports),
                    GetImportsClauses(newRoot.Imports),
                    parent,
                    CodeModelEventType.Unknown,
                    eventQueue)

                ' File-level attributes
                CompareChildren(
                    AddressOf CompareAttributes,
                    GetAttributes(oldRoot.Attributes),
                    GetAttributes(newRoot.Attributes),
                    parent,
                    CodeModelEventType.Unknown,
                    eventQueue)

                ' Namespaces and types
                CompareChildren(
                    AddressOf CompareNamespacesOrTypes,
                    GetMembers(oldRoot.Members),
                    GetMembers(newRoot.Members),
                    parent,
                    CodeModelEventType.Unknown,
                    eventQueue)
            End Sub

            Private Shared Function GetImportsClauses(importsStatements As SyntaxList(Of ImportsStatementSyntax)) As IReadOnlyList(Of ImportsClauseSyntax)
                Return importsStatements _
                    .SelectMany(Function(i) i.ImportsClauses) _
                    .Where(Function(i) Not TypeOf i Is XmlNamespaceImportsClauseSyntax) _
                    .ToArray()
            End Function

            Private Shared Function GetAttributes(attributesStatements As SyntaxList(Of AttributesStatementSyntax)) As IReadOnlyList(Of AttributeSyntax)
                Return attributesStatements _
                    .SelectMany(Function(a) a.AttributeLists) _
                    .SelectMany(Function(a) a.Attributes) _
                    .ToArray()
            End Function

            Private Shared Function GetAttributes(attributeLists As SyntaxList(Of AttributeListSyntax)) As IReadOnlyList(Of AttributeSyntax)
                Return attributeLists _
                    .SelectMany(Function(a) a.Attributes) _
                    .ToArray()
            End Function

            Private Shared Function IsMissingEndBlockError(statement As DeclarationStatementSyntax, [error] As String) As Boolean
                Select Case [error]
                    Case "BC30481" ' Missing End Class
                        Return True
                    Case "BC30625" ' Missing End Module
                        Return True
                    Case "BC30185" ' Missing End Enum
                        Return True
                    Case "BC30253" ' Missing End Interface
                        Return True
                    Case "BC30624" ' Missing End Structure
                        Return True
                    Case "BC30626" ' Missing End Namespace
                        Return True
                    Case "BC30026" ' Missing End Sub
                        Return True
                    Case "BC30027" ' Missing End Function
                        Return True
                    Case "BC30025" ' Missing End Property
                        Return True
                    Case "BC33005" ' Missing End Operator
                        Return True
                End Select

                Return False
            End Function

            Private Shared Function HasOnlyMissingEndBlockErrors(statement As DeclarationStatementSyntax) As Boolean
                Dim errors = statement.GetDiagnostics()
                If Not errors.Any() Then
                    Return True
                End If

                Return errors.All(Function(e) IsMissingEndBlockError(statement, e.Id))
            End Function

            Private Shared Function IsValidTopLevelDeclaration(member As DeclarationStatementSyntax) As Boolean
                If member.IsTopLevelBlock() Then
                    Dim memberBegin = member.GetTopLevelBlockBegin()
                    If memberBegin.IsTopLevelDeclaration() AndAlso HasOnlyMissingEndBlockErrors(memberBegin) Then
                        Return True
                    End If
                ElseIf member.IsTopLevelDeclaration() Then
                    If member.ContainsDiagnostics Then
                        Return False
                    End If

                    If member.Parent.IsKind(SyntaxKind.CompilationUnit, SyntaxKind.NamespaceBlock) AndAlso
                       member.IsKind(SyntaxKind.FieldDeclaration) Then

                        Return False
                    End If

                    Return True
                End If

                Return False
            End Function

            Private Shared Function GetMembers(members As SyntaxList(Of StatementSyntax)) As IReadOnlyList(Of DeclarationStatementSyntax)
                Return members _
                    .OfType(Of DeclarationStatementSyntax) _
                    .Where(Function(m) IsValidTopLevelDeclaration(m)) _
                    .ToArray()
            End Function

            Private Shared Function GetNames(variableDeclarators As SeparatedSyntaxList(Of VariableDeclaratorSyntax)) As IReadOnlyList(Of ModifiedIdentifierSyntax)
                Return variableDeclarators _
                    .SelectMany(Function(v) v.Names) _
                    .ToArray()
            End Function

            Private Function GetParameters(parameterList As ParameterListSyntax) As SeparatedSyntaxList(Of ParameterSyntax)
                Return If(parameterList IsNot Nothing,
                          parameterList.Parameters,
                          Nothing)
            End Function

            Private Function CompareOptions(oldOption As OptionStatementSyntax, newOption As OptionStatementSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                If oldOption.NameKeyword.Kind <> newOption.NameKeyword.Kind OrElse
                   oldOption.ValueKeyword.Kind <> newOption.ValueKeyword.Kind Then

                    EnqueueChangeEvent(newOption, newNodeParent, CodeModelEventType.Rename, eventQueue)
                    Return False
                End If

                Return True
            End Function

            Private Function CompareImportsClauses(oldImportsClause As ImportsClauseSyntax, newImportsClause As ImportsClauseSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                ' Note: Only namespaces are compared and aliases are ignored.
                If Not CompareNames(oldImportsClause.GetName(), newImportsClause.GetName()) Then
                    EnqueueChangeEvent(newImportsClause, newNodeParent, CodeModelEventType.Rename, eventQueue)
                    Return False
                End If

                Return True
            End Function

            Private Function CompareAttributes(oldAttribute As AttributeSyntax, newAttribute As AttributeSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Dim hasChanges = False

                Dim targetsChange As CodeModelEventType = 0
                Dim namesChange As CodeModelEventType = 0
                Dim argumentsChange As CodeModelEventType = 0

                If Not CompareAttributeTargets(oldAttribute.Target, newAttribute.Target) Then
                    targetsChange = CodeModelEventType.Unknown
                    hasChanges = True
                End If

                If Not CompareTypeNames(oldAttribute.Name, newAttribute.Name) Then
                    namesChange = CodeModelEventType.Rename
                    hasChanges = True
                End If

                If Not CompareArgumentLists(oldAttribute.ArgumentList, newAttribute.ArgumentList, newAttribute, eventQueue) Then
                    argumentsChange = CodeModelEventType.ArgChange
                    hasChanges = True
                End If

                If hasChanges Then
                    EnqueueChangeEvent(newAttribute, newNodeParent, targetsChange Or namesChange Or argumentsChange, eventQueue)
                End If

                Return Not hasChanges
            End Function

            Private Function CompareAttributeTargets(oldAttributeTarget As AttributeTargetSyntax, newAttributeTarget As AttributeTargetSyntax) As Boolean
                If oldAttributeTarget Is Nothing OrElse newAttributeTarget Is Nothing Then
                    Return oldAttributeTarget Is newAttributeTarget
                End If

                Return oldAttributeTarget.AttributeModifier.Kind = newAttributeTarget.AttributeModifier.Kind
            End Function

            Private Function CompareArgumentLists(oldArgumentList As ArgumentListSyntax, newArgumentList As ArgumentListSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Dim oldArguments = If(oldArgumentList IsNot Nothing, oldArgumentList.Arguments, Nothing)
                Dim newArguments = If(newArgumentList IsNot Nothing, newArgumentList.Arguments, Nothing)

                Return CompareChildren(
                    AddressOf CompareArguments,
                    oldArguments.AsReadOnlyList(),
                    newArguments.AsReadOnlyList(),
                    newNodeParent,
                    CodeModelEventType.Unknown,
                    eventQueue)
            End Function

            Private Function CompareArguments(oldArgument As ArgumentSyntax, newArgument As ArgumentSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Debug.Assert((TypeOf oldArgument Is SimpleArgumentSyntax OrElse
                              TypeOf oldArgument Is OmittedArgumentSyntax) AndAlso
                             (TypeOf newArgument Is SimpleArgumentSyntax OrElse
                              TypeOf newArgument Is OmittedArgumentSyntax))

                Dim hasChanges = False

                Dim nameChanges As CodeModelEventType = 0
                Dim valueChanges As CodeModelEventType = 0

                If oldArgument.Kind <> newArgument.Kind OrElse
                   oldArgument.IsNamed <> newArgument.IsNamed _
                Then
                    nameChanges = CodeModelEventType.Rename
                    hasChanges = True
                ElseIf oldArgument.IsNamed Then
                    Dim oldNamedArgument = DirectCast(oldArgument, SimpleArgumentSyntax)
                    Dim newNamedArgument = DirectCast(newArgument, SimpleArgumentSyntax)

                    If Not CompareNames(oldNamedArgument.NameColonEquals.Name, newNamedArgument.NameColonEquals.Name) Then
                        nameChanges = CodeModelEventType.Rename
                        hasChanges = True
                    End If
                End If

                Dim oldExpression = oldArgument.GetExpression()
                Dim newExpression = newArgument.GetExpression()

                If Not CompareExpressions(oldExpression, newExpression) Then
                    valueChanges = CodeModelEventType.Unknown
                    hasChanges = True
                End If

                If hasChanges Then
                    EnqueueChangeEvent(newArgument, newNodeParent, nameChanges Or valueChanges, eventQueue)
                End If

                Return Not hasChanges
            End Function

            Private Function CompareNamespacesOrTypes(oldNamespaceOrType As DeclarationStatementSyntax, newNamespaceOrType As DeclarationStatementSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                ' If the kind doesn't match, it has to be a remove/add.
                If oldNamespaceOrType.Kind <> newNamespaceOrType.Kind Then
                    EnqueueRemoveEvent(oldNamespaceOrType, newNodeParent, eventQueue)
                    EnqueueAddEvent(newNamespaceOrType, newNodeParent, eventQueue)

                    Return False
                End If

                If TypeOf oldNamespaceOrType Is IncompleteMemberSyntax Then
                    Return False
                End If

                If TypeOf oldNamespaceOrType Is NamespaceBlockSyntax Then

                    Return CompareNamespaces(
                        DirectCast(oldNamespaceOrType, NamespaceBlockSyntax),
                        DirectCast(newNamespaceOrType, NamespaceBlockSyntax),
                        newNodeParent,
                        eventQueue)

                ElseIf TypeOf oldNamespaceOrType Is TypeBlockSyntax OrElse
                       TypeOf oldNamespaceOrType Is EnumBlockSyntax OrElse
                       TypeOf oldNamespaceOrType Is DelegateStatementSyntax Then

                    Return CompareTypeDeclarations(
                        oldNamespaceOrType,
                        newNamespaceOrType,
                        newNodeParent,
                        eventQueue)
                End If

                Return False
            End Function

            Private Function CompareNamespaces(oldNamespace As NamespaceBlockSyntax, newNamespace As NamespaceBlockSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                If Not CompareNames(oldNamespace.NamespaceStatement.Name, newNamespace.NamespaceStatement.Name) Then
                    Dim change = CompareRenamedDeclarations(
                        AddressOf CompareNamespacesOrTypes,
                        GetMembers(oldNamespace.Members),
                        GetMembers(newNamespace.Members),
                        oldNamespace,
                        newNamespace,
                        newNodeParent,
                        eventQueue)

                    If change = DeclarationChange.NameOnly Then
                        EnqueueChangeEvent(newNamespace, newNodeParent, CodeModelEventType.Rename, eventQueue)
                    End If

                    Return False
                End If

                Return CompareChildren(
                    AddressOf CompareNamespacesOrTypes,
                    GetMembers(oldNamespace.Members),
                    GetMembers(newNamespace.Members),
                    newNamespace,
                    CodeModelEventType.Unknown,
                    eventQueue)
            End Function

            Private Function TypeKindChanged(oldType As DeclarationStatementSyntax, newType As DeclarationStatementSyntax) As Boolean
                ' Several differences in member kind should not cause an add/remove event pair. For example, changing a Sub to a Function.

                If oldType.IsKind(SyntaxKind.DeclareFunctionStatement, SyntaxKind.DelegateSubStatement) AndAlso
                   newType.IsKind(SyntaxKind.DeclareFunctionStatement, SyntaxKind.DelegateSubStatement) Then
                    Return False
                End If

                Return oldType.Kind <> newType.Kind
            End Function

            Private Function CompareTypeDeclarations(oldType As DeclarationStatementSyntax, newType As DeclarationStatementSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Debug.Assert(oldType IsNot Nothing AndAlso newType IsNot Nothing)
                Debug.Assert(TypeOf oldType Is TypeBlockSyntax OrElse TypeOf oldType Is EnumBlockSyntax OrElse TypeOf oldType Is DelegateStatementSyntax)
                Debug.Assert(TypeOf newType Is TypeBlockSyntax OrElse TypeOf oldType Is EnumBlockSyntax OrElse TypeOf newType Is DelegateStatementSyntax)

                ' If the kind doesn't match, it has to be a remove/add.
                If TypeKindChanged(oldType, newType) Then
                    EnqueueRemoveEvent(oldType, newNodeParent, eventQueue)
                    EnqueueAddEvent(newType, newNodeParent, eventQueue)

                    Return False
                End If

                If TypeOf oldType Is TypeBlockSyntax Then
                    Return CompareTypes(
                        DirectCast(oldType, TypeBlockSyntax),
                        DirectCast(newType, TypeBlockSyntax),
                        newNodeParent,
                        eventQueue)
                ElseIf TypeOf oldType Is EnumBlockSyntax Then
                    Return CompareEnums(
                        DirectCast(oldType, EnumBlockSyntax),
                        DirectCast(newType, EnumBlockSyntax),
                        newNodeParent,
                        eventQueue)
                ElseIf TypeOf oldType Is DelegateStatementSyntax Then
                    Return CompareMethods(
                        DirectCast(oldType, DelegateStatementSyntax),
                        DirectCast(newType, DelegateStatementSyntax),
                        newNodeParent,
                        eventQueue)
                End If

                Return False
            End Function

            Private Function CompareTypes(oldType As TypeBlockSyntax, newType As TypeBlockSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Dim hasChanges = False

                Dim namesChange As CodeModelEventType = 0
                Dim modifiersChange As CodeModelEventType = 0
                Dim baseListsChange As CodeModelEventType = 0

                If Not StringComparer.OrdinalIgnoreCase.Equals(oldType.BlockStatement.Identifier.ToString(), newType.BlockStatement.Identifier.ToString()) Then
                    ' If the type name is different, it might mean that the whole type has been removed a new one added.
                    ' In that case, we shouldn't do any other checks and return immediately.
                    Dim change = CompareRenamedDeclarations(
                        AddressOf CompareMemberDeclarations,
                        GetMembers(oldType.Members),
                        GetMembers(newType.Members),
                        oldType,
                        newType,
                        newNodeParent, eventQueue)

                    If change = DeclarationChange.WholeDeclaration Then
                        Return False
                    End If

                    namesChange = CodeModelEventType.Rename
                    hasChanges = True
                End If

                If Not CompareModifiers(oldType, newType) Then
                    modifiersChange = CodeModelEventType.Unknown
                    hasChanges = True
                End If

                If Not CompareBaseLists(oldType, newType, eventQueue) Then
                    baseListsChange = CodeModelEventType.BaseChange
                    hasChanges = True
                End If

                Dim comp1 = CompareChildren(
                    AddressOf CompareAttributes,
                    GetAttributes(oldType.BlockStatement.AttributeLists),
                    GetAttributes(newType.BlockStatement.AttributeLists),
                    newType,
                    CodeModelEventType.Unknown,
                    eventQueue)

                Dim comp2 = CompareChildren(
                    AddressOf CompareMemberDeclarations,
                    GetMembers(oldType.Members),
                    GetMembers(newType.Members),
                    newType,
                    CodeModelEventType.Unknown,
                    eventQueue)

                If hasChanges Then
                    EnqueueChangeEvent(newType, newNodeParent, namesChange Or modifiersChange Or baseListsChange, eventQueue)
                End If

                If Not comp1 OrElse Not comp2 Then
                    hasChanges = True
                End If

                Return Not hasChanges
            End Function

            Private Function CompareEnums(oldEnum As EnumBlockSyntax, newEnum As EnumBlockSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Dim hasChanges = False

                Dim namesChange As CodeModelEventType = 0
                Dim modifiersChange As CodeModelEventType = 0
                Dim baseListsChange As CodeModelEventType = 0

                If Not StringComparer.OrdinalIgnoreCase.Equals(oldEnum.EnumStatement.Identifier.ToString(), newEnum.EnumStatement.Identifier.ToString()) Then
                    ' If the type name is different, it might mean that the whole type has been removed a new one added.
                    ' In that case, we shouldn't do any other checks and return immediately.
                    Dim change = CompareRenamedDeclarations(
                        AddressOf CompareMemberDeclarations,
                        GetMembers(oldEnum.Members),
                        GetMembers(newEnum.Members),
                        oldEnum,
                        newEnum,
                        newNodeParent, eventQueue)

                    If change = DeclarationChange.WholeDeclaration Then
                        Return False
                    End If

                    namesChange = CodeModelEventType.Rename
                    hasChanges = True
                End If

                If Not CompareModifiers(oldEnum, newEnum) Then
                    modifiersChange = CodeModelEventType.Unknown
                    hasChanges = True
                End If

                Dim comp1 = CompareChildren(
                    AddressOf CompareAttributes,
                    GetAttributes(oldEnum.EnumStatement.AttributeLists),
                    GetAttributes(newEnum.EnumStatement.AttributeLists),
                    newEnum,
                    CodeModelEventType.Unknown,
                    eventQueue)

                Dim comp2 = CompareChildren(
                    AddressOf CompareMemberDeclarations,
                    GetMembers(oldEnum.Members),
                    GetMembers(newEnum.Members),
                    newEnum,
                    CodeModelEventType.Unknown,
                    eventQueue)

                If hasChanges Then
                    EnqueueChangeEvent(newEnum, newNodeParent, namesChange Or modifiersChange Or baseListsChange, eventQueue)
                End If

                If Not comp1 OrElse Not comp2 Then
                    hasChanges = True
                End If

                Return Not hasChanges
            End Function

            Private Function MemberKindChanged(oldMember As StatementSyntax, newMember As StatementSyntax) As Boolean
                ' Several differences in member kind should not cause an add/remove event pair. For example, changing a Sub to a Function.

                If oldMember.IsKind(SyntaxKind.SubStatement, SyntaxKind.FunctionStatement, SyntaxKind.SubBlock, SyntaxKind.FunctionBlock) AndAlso
                   newMember.IsKind(SyntaxKind.SubStatement, SyntaxKind.FunctionStatement, SyntaxKind.SubBlock, SyntaxKind.FunctionBlock) Then
                    Return False
                End If

                If oldMember.IsKind(SyntaxKind.DeclareFunctionStatement, SyntaxKind.DeclareSubStatement) AndAlso
                   newMember.IsKind(SyntaxKind.DeclareFunctionStatement, SyntaxKind.DeclareSubStatement) Then
                    Return False
                End If

                If oldMember.IsKind(SyntaxKind.PropertyStatement, SyntaxKind.PropertyBlock) AndAlso
                   newMember.IsKind(SyntaxKind.PropertyStatement, SyntaxKind.PropertyBlock) Then
                    Return False
                End If

                If oldMember.IsKind(SyntaxKind.EventStatement, SyntaxKind.EventBlock) AndAlso
                   newMember.IsKind(SyntaxKind.EventStatement, SyntaxKind.EventBlock) Then
                    Return False
                End If

                Return oldMember.Kind <> newMember.Kind
            End Function

            Private Function CompareMemberDeclarations(oldMember As DeclarationStatementSyntax, newMember As DeclarationStatementSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Debug.Assert(oldMember IsNot Nothing AndAlso newMember IsNot Nothing)

                ' If the kind doesn't match, it has to be a remove/add.
                If MemberKindChanged(oldMember, newMember) Then
                    EnqueueRemoveEvent(oldMember, newNodeParent, eventQueue)
                    EnqueueAddEvent(newMember, newNodeParent, eventQueue)

                    Return False
                End If

                If TypeOf oldMember Is TypeBlockSyntax OrElse
                   TypeOf oldMember Is EnumBlockSyntax OrElse
                   TypeOf oldMember Is DelegateStatementSyntax Then

                    Return CompareTypeDeclarations(oldMember, newMember, newNodeParent, eventQueue)

                    ' oldMember should be checked for PropertyStatementSyntax and EventStatementSyntax before being checked for MethodBaseSyntax since
                    ' sometimes newMember could be a PropertyBlockSyntax or EventBlockSyntax which neither MethodBlockBaseSyntax nor MethodBaseSyntax
                ElseIf TypeOf oldMember Is PropertyBlockSyntax OrElse
                       TypeOf oldMember Is PropertyStatementSyntax Then
                    Return CompareProperties(
                        If(TypeOf oldMember Is PropertyBlockSyntax, DirectCast(oldMember, PropertyBlockSyntax).PropertyStatement, DirectCast(oldMember, PropertyStatementSyntax)),
                        If(TypeOf newMember Is PropertyBlockSyntax, DirectCast(newMember, PropertyBlockSyntax).PropertyStatement, DirectCast(newMember, PropertyStatementSyntax)),
                        newNodeParent,
                        eventQueue)
                ElseIf TypeOf oldMember Is EventBlockSyntax OrElse
                       TypeOf oldMember Is EventStatementSyntax Then
                    Return CompareEvents(
                        If(TypeOf oldMember Is EventBlockSyntax, DirectCast(oldMember, EventBlockSyntax).EventStatement, DirectCast(oldMember, EventStatementSyntax)),
                        If(TypeOf newMember Is EventBlockSyntax, DirectCast(newMember, EventBlockSyntax).EventStatement, DirectCast(newMember, EventStatementSyntax)),
                        newNodeParent,
                        eventQueue)
                ElseIf TypeOf oldMember Is MethodBlockBaseSyntax OrElse
                       TypeOf oldMember Is MethodBaseSyntax Then
                    Return CompareMethods(
                        If(TypeOf oldMember Is MethodBlockBaseSyntax, DirectCast(oldMember, MethodBlockBaseSyntax).BlockStatement, DirectCast(oldMember, MethodBaseSyntax)),
                        If(TypeOf newMember Is MethodBlockBaseSyntax, DirectCast(newMember, MethodBlockBaseSyntax).BlockStatement, DirectCast(newMember, MethodBaseSyntax)),
                        newNodeParent,
                        eventQueue)
                ElseIf TypeOf oldMember Is FieldDeclarationSyntax Then
                    Return CompareFields(
                        DirectCast(oldMember, FieldDeclarationSyntax),
                        DirectCast(newMember, FieldDeclarationSyntax),
                        newNodeParent,
                        eventQueue)
                ElseIf TypeOf oldMember Is EnumMemberDeclarationSyntax Then
                    Return CompareEnumMembers(
                        DirectCast(oldMember, EnumMemberDeclarationSyntax),
                        DirectCast(newMember, EnumMemberDeclarationSyntax),
                        newNodeParent,
                        eventQueue)
                End If

                Debug.Fail(String.Format("Unexpected member kind: {0}", oldMember.Kind))
                Throw New NotImplementedException()
            End Function

            Private Function CompareMethods(oldMethod As MethodBaseSyntax, newMethod As MethodBaseSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Dim hasChanges = False

                Dim namesChange As CodeModelEventType = 0
                Dim modifiersChange As CodeModelEventType = 0
                Dim typesChange As CodeModelEventType = 0

                If Not StringComparer.OrdinalIgnoreCase.Equals(oldMethod.GetNameText(), newMethod.GetNameText()) Then
                    ' If the method name is different, it might mean that the whole method has been removed a new one added.
                    ' In that case, we shouldn't do any other checks and return immediately.
                    Dim change = CompareRenamedDeclarations(
                        AddressOf CompareParameters,
                        GetParameters(oldMethod.ParameterList).AsReadOnlyList(),
                        GetParameters(newMethod.ParameterList).AsReadOnlyList(),
                        GetValidParentNode(oldMethod),
                        GetValidParentNode(newMethod),
                        newNodeParent, eventQueue)

                    If change = DeclarationChange.WholeDeclaration Then
                        Return False
                    End If

                    namesChange = CodeModelEventType.Rename
                    hasChanges = True
                End If

                If Not CompareModifiers(oldMethod, newMethod) Then
                    modifiersChange = CodeModelEventType.Unknown
                    hasChanges = True
                End If

                If Not CompareTypeNames(oldMethod.Type(), newMethod.Type()) Then
                    typesChange = CodeModelEventType.TypeRefChange
                    hasChanges = True
                End If

                Dim comp1 = CompareChildren(
                    AddressOf CompareAttributes,
                    GetAttributes(oldMethod.AttributeLists),
                    GetAttributes(newMethod.AttributeLists),
                    GetValidParentNode(newMethod),
                    CodeModelEventType.Unknown,
                    eventQueue)

                Dim comp2 = CompareParameterLists(
                    oldMethod.ParameterList,
                    newMethod.ParameterList,
                    GetValidParentNode(newMethod),
                    eventQueue)

                If hasChanges Then
                    EnqueueChangeEvent(newMethod, newNodeParent, namesChange Or modifiersChange Or typesChange, eventQueue)
                End If

                If Not comp1 OrElse Not comp2 Then
                    hasChanges = True
                End If

                Return Not hasChanges
            End Function

            Private Function CompareProperties(oldProperty As PropertyStatementSyntax, newProperty As PropertyStatementSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Dim hasChanges = False

                Dim namesChange As CodeModelEventType = 0
                Dim modifiersChange As CodeModelEventType = 0
                Dim typesChange As CodeModelEventType = 0

                If Not StringComparer.OrdinalIgnoreCase.Equals(oldProperty.Identifier.ToString(), newProperty.Identifier.ToString()) Then
                    ' If the property name is different, it might mean that the property method has been removed a new one added.
                    ' In that case, we shouldn't do any other checks and return immediately.
                    Dim change = CompareRenamedDeclarations(
                        AddressOf CompareParameters,
                        GetParameters(oldProperty.ParameterList).AsReadOnlyList(),
                        GetParameters(newProperty.ParameterList).AsReadOnlyList(),
                        GetValidParentNode(oldProperty),
                        GetValidParentNode(newProperty),
                        newNodeParent, eventQueue)

                    If change = DeclarationChange.WholeDeclaration Then
                        Return False
                    End If

                    namesChange = CodeModelEventType.Rename
                    hasChanges = True
                End If

                If Not CompareModifiers(oldProperty, newProperty) Then
                    modifiersChange = CodeModelEventType.Unknown
                    hasChanges = True
                End If

                If Not CompareTypeNames(oldProperty.Type(), newProperty.Type()) Then
                    typesChange = CodeModelEventType.TypeRefChange
                    hasChanges = True
                End If

                Dim comp1 = CompareChildren(
                    AddressOf CompareAttributes,
                    GetAttributes(oldProperty.AttributeLists),
                    GetAttributes(newProperty.AttributeLists),
                    GetValidParentNode(newProperty),
                    CodeModelEventType.Unknown,
                    eventQueue)

                Dim comp2 = CompareParameterLists(
                    oldProperty.ParameterList,
                    newProperty.ParameterList,
                    GetValidParentNode(newProperty),
                    eventQueue)

                If hasChanges Then
                    EnqueueChangeEvent(newProperty, newNodeParent, namesChange Or modifiersChange Or typesChange, eventQueue)
                End If

                If Not comp1 OrElse Not comp2 Then
                    hasChanges = True
                End If

                Return Not hasChanges
            End Function

            Private Function CompareEvents(oldEvent As EventStatementSyntax, newEvent As EventStatementSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Dim hasChanges = False

                Dim namesChange As CodeModelEventType = 0
                Dim modifiersChange As CodeModelEventType = 0
                Dim typesChange As CodeModelEventType = 0

                If Not StringComparer.OrdinalIgnoreCase.Equals(oldEvent.Identifier.ToString(), newEvent.Identifier.ToString()) Then
                    ' If the property name is different, it might mean that the property method has been removed a new one added.
                    ' In that case, we shouldn't do any other checks and return immediately.
                    Dim change = CompareRenamedDeclarations(
                        AddressOf CompareParameters,
                        GetParameters(oldEvent.ParameterList).AsReadOnlyList(),
                        GetParameters(newEvent.ParameterList).AsReadOnlyList(),
                        GetValidParentNode(oldEvent),
                        GetValidParentNode(newEvent),
                        newNodeParent, eventQueue)

                    If change = DeclarationChange.WholeDeclaration Then
                        Return False
                    End If

                    namesChange = CodeModelEventType.Rename
                    hasChanges = True
                End If

                If Not CompareModifiers(oldEvent, newEvent) Then
                    modifiersChange = CodeModelEventType.Unknown
                    hasChanges = True
                End If

                If Not CompareTypeNames(oldEvent.Type(), newEvent.Type()) Then
                    typesChange = CodeModelEventType.TypeRefChange
                    hasChanges = True
                End If

                Dim comp1 = CompareChildren(
                    AddressOf CompareAttributes,
                    GetAttributes(oldEvent.AttributeLists),
                    GetAttributes(newEvent.AttributeLists),
                    GetValidParentNode(newEvent),
                    CodeModelEventType.Unknown,
                    eventQueue)

                Dim comp2 = CompareParameterLists(
                    oldEvent.ParameterList,
                    newEvent.ParameterList,
                    GetValidParentNode(newEvent),
                    eventQueue)

                If hasChanges Then
                    EnqueueChangeEvent(newEvent, newNodeParent, namesChange Or modifiersChange Or typesChange, eventQueue)
                End If

                If Not comp1 OrElse Not comp2 Then
                    hasChanges = True
                End If

                Return Not hasChanges
            End Function

            Private Function CompareFields(oldField As FieldDeclarationSyntax, newField As FieldDeclarationSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Debug.Assert(oldField IsNot Nothing AndAlso newField IsNot Nothing)

                Dim hasChanges = False

                If CompareChildren(
                    AddressOf CompareModifiedIdentifiers,
                    GetNames(oldField.Declarators),
                    GetNames(newField.Declarators),
                    newNodeParent,
                    CodeModelEventType.Unknown,
                    eventQueue) Then

                    hasChanges = True
                End If

                If CompareChildren(
                    AddressOf CompareAttributes,
                    GetAttributes(oldField.AttributeLists),
                    GetAttributes(newField.AttributeLists),
                    newField,
                    CodeModelEventType.Unknown,
                    eventQueue) Then

                    hasChanges = True
                End If

                Return hasChanges
            End Function

            Private Function CompareModifiedIdentifiers(oldModifiedIdentifier As ModifiedIdentifierSyntax, newModifiedIdentifier As ModifiedIdentifierSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Debug.Assert(oldModifiedIdentifier IsNot Nothing AndAlso newModifiedIdentifier IsNot Nothing)

                Dim hasChanges = False

                Dim namesChange As CodeModelEventType = 0
                Dim typesChange As CodeModelEventType = 0
                Dim modifiersChange As CodeModelEventType = 0

                If Not StringComparer.OrdinalIgnoreCase.Equals(oldModifiedIdentifier.Identifier.ToString(), newModifiedIdentifier.Identifier.ToString()) Then
                    namesChange = CodeModelEventType.Rename
                    Return False
                End If

                Dim oldVariableDeclarator = DirectCast(oldModifiedIdentifier.Parent, VariableDeclaratorSyntax)
                Dim newVariableDeclarator = DirectCast(newModifiedIdentifier.Parent, VariableDeclaratorSyntax)

                If Not CompareTypeNames(oldVariableDeclarator.Type(), newVariableDeclarator.Type()) Then
                    typesChange = CodeModelEventType.TypeRefChange
                    hasChanges = True
                End If

                Dim oldField = DirectCast(oldVariableDeclarator.Parent, FieldDeclarationSyntax)
                Dim newField = DirectCast(newVariableDeclarator.Parent, FieldDeclarationSyntax)

                If Not CompareModifiers(oldField, newField) Then
                    modifiersChange = CodeModelEventType.Unknown
                    hasChanges = True
                End If

                If hasChanges Then
                    EnqueueChangeEvent(newModifiedIdentifier, newNodeParent, namesChange Or typesChange Or modifiersChange, eventQueue)
                End If

                Return Not hasChanges
            End Function

            Private Function CompareEnumMembers(oldEnumMember As EnumMemberDeclarationSyntax, newEnumMember As EnumMemberDeclarationSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Debug.Assert(oldEnumMember IsNot Nothing AndAlso newEnumMember IsNot Nothing)

                Dim hasChanges = False

                Dim namesChange As CodeModelEventType = 0

                If Not StringComparer.Ordinal.Equals(oldEnumMember.Identifier.ToString(), newEnumMember.Identifier.ToString()) Then
                    namesChange = CodeModelEventType.Rename
                    hasChanges = True
                End If

                Dim comp1 = CompareChildren(
                    AddressOf CompareAttributes,
                    GetAttributes(oldEnumMember.AttributeLists),
                    GetAttributes(newEnumMember.AttributeLists),
                    newEnumMember,
                    CodeModelEventType.Unknown,
                    eventQueue)

                If hasChanges Then
                    EnqueueChangeEvent(newEnumMember, newNodeParent, namesChange, eventQueue)
                End If

                If Not comp1 Then
                    hasChanges = True
                End If

                Return Not hasChanges
            End Function

            Private Function CompareParameterLists(oldParameterList As ParameterListSyntax, newParameterList As ParameterListSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Dim oldParameters = GetParameters(oldParameterList)
                Dim newParameters = GetParameters(newParameterList)

                Return CompareChildren(
                    AddressOf CompareParameters,
                    oldParameters.AsReadOnlyList(),
                    newParameters.AsReadOnlyList(),
                    newNodeParent,
                    CodeModelEventType.Unknown,
                    eventQueue)
            End Function

            Private Function CompareParameters(oldParameter As ParameterSyntax, newParameter As ParameterSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Debug.Assert(oldParameter IsNot Nothing AndAlso newParameter IsNot Nothing)

                Dim hasChanges = False

                Dim namesChange As CodeModelEventType = 0
                Dim modifiersChange As CodeModelEventType = 0
                Dim typesChange As CodeModelEventType = 0
                Dim valuesChange As CodeModelEventType = 0

                If Not StringComparer.OrdinalIgnoreCase.Equals(Me.CodeModelService.GetParameterName(oldParameter), Me.CodeModelService.GetParameterName(newParameter)) Then
                    namesChange = CodeModelEventType.Rename
                    hasChanges = True
                End If

                If Not CompareModifiers(oldParameter, newParameter) Then
                    modifiersChange = CodeModelEventType.Unknown
                    hasChanges = True
                End If

                If Not CompareTypeNames(oldParameter.Type(), newParameter.Type()) Then
                    typesChange = CodeModelEventType.TypeRefChange
                    hasChanges = True
                End If

                If hasChanges Then
                    EnqueueChangeEvent(newParameter, newNodeParent, namesChange Or modifiersChange Or valuesChange Or typesChange, eventQueue)
                End If

                Return Not hasChanges
            End Function

            Private Function CompareBaseLists(oldType As TypeBlockSyntax, newType As TypeBlockSyntax, eventQueue As CodeModelEventQueue) As Boolean
                Dim comp1 = CompareChildren(
                    AddressOf CompareInherits,
                    oldType.Inherits.AsReadOnlyList(),
                    newType.Inherits.AsReadOnlyList(),
                    newType,
                    CodeModelEventType.Unknown,
                    eventQueue)

                Dim comp2 = CompareChildren(
                    AddressOf CompareImplements,
                    oldType.Implements.AsReadOnlyList(),
                    newType.Implements.AsReadOnlyList(),
                    newType,
                    CodeModelEventType.Unknown,
                    eventQueue)

                Return comp1 AndAlso comp2
            End Function

            Private Function CompareInherits(oldInherits As InheritsStatementSyntax, newInherits As InheritsStatementSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Return True
            End Function

            Private Function CompareImplements(oldImplements As ImplementsStatementSyntax, newImplements As ImplementsStatementSyntax, newNodeParent As SyntaxNode, eventQueue As CodeModelEventQueue) As Boolean
                Return True
            End Function

            Private Function CompareModifiers(oldMember As StatementSyntax, newMember As StatementSyntax) As Boolean
                Return oldMember.GetModifierFlags() = newMember.GetModifierFlags()
            End Function

            Private Function CompareModifiers(oldParameter As ParameterSyntax, newParameter As ParameterSyntax) As Boolean
                Return oldParameter.GetModifierFlags() = newParameter.GetModifierFlags()
            End Function

            Private Function CompareExpressions(oldExpression As ExpressionSyntax, newExpression As ExpressionSyntax) As Boolean
                If oldExpression Is Nothing OrElse newExpression Is Nothing Then
                    Return oldExpression Is newExpression
                End If

                If oldExpression.Kind <> newExpression.Kind Then
                    Return False
                End If

                If TypeOf oldExpression Is TypeSyntax Then
                    Return CompareTypeNames(DirectCast(oldExpression, TypeSyntax), DirectCast(newExpression, TypeSyntax))
                End If

                If TypeOf oldExpression Is LiteralExpressionSyntax Then
                    Return StringComparer.OrdinalIgnoreCase.Equals(oldExpression.ToString(), newExpression.ToString())
                End If

                If TypeOf oldExpression Is CastExpressionSyntax Then
                    Dim oldCast = DirectCast(oldExpression, CastExpressionSyntax)
                    Dim newCast = DirectCast(newExpression, CastExpressionSyntax)

                    Return CompareTypeNames(oldCast.Type, newCast.Type) AndAlso
                           CompareExpressions(oldCast.Expression, newCast.Expression)
                End If

                If TypeOf oldExpression Is PredefinedCastExpressionSyntax Then
                    Dim oldPredefinedCast = DirectCast(oldExpression, PredefinedCastExpressionSyntax)
                    Dim newPredefinedCast = DirectCast(newExpression, PredefinedCastExpressionSyntax)

                    Return CompareExpressions(oldPredefinedCast.Expression, newPredefinedCast.Expression)
                End If

                If TypeOf oldExpression Is UnaryExpressionSyntax Then
                    Dim oldUnaryExpression = DirectCast(oldExpression, UnaryExpressionSyntax)
                    Dim newUnaryExpression = DirectCast(newExpression, UnaryExpressionSyntax)

                    Return CompareExpressions(oldUnaryExpression.Operand, newUnaryExpression.Operand)
                End If

                If TypeOf oldExpression Is BinaryExpressionSyntax Then
                    Dim oldBinaryExpression = DirectCast(oldExpression, BinaryExpressionSyntax)
                    Dim newBinaryExpression = DirectCast(newExpression, BinaryExpressionSyntax)

                    Return CompareExpressions(oldBinaryExpression.Left, newBinaryExpression.Left) AndAlso
                           CompareExpressions(oldBinaryExpression.Right, newBinaryExpression.Right)
                End If

                If TypeOf oldExpression Is MemberAccessExpressionSyntax Then
                    Dim oldMemberAccess = DirectCast(oldExpression, MemberAccessExpressionSyntax)
                    Dim newMemberAccess = DirectCast(newExpression, MemberAccessExpressionSyntax)

                    Return CompareExpressions(oldMemberAccess.Expression, newMemberAccess.Expression) AndAlso
                           CompareExpressions(oldMemberAccess.Name, newMemberAccess.Name)
                End If

                Return True
            End Function

            Private Function CompareTypeNames(oldType As TypeSyntax, newType As TypeSyntax) As Boolean
                If oldType Is Nothing OrElse newType Is Nothing Then
                    Return oldType Is newType
                End If

                If oldType.Kind <> newType.Kind Then
                    Return False
                End If

                Select Case oldType.Kind
                    Case SyntaxKind.PredefinedType
                        Dim oldPredefinedType = DirectCast(oldType, PredefinedTypeSyntax)
                        Dim newPredefinedType = DirectCast(newType, PredefinedTypeSyntax)

                        Return oldPredefinedType.Keyword.Kind = newPredefinedType.Keyword.Kind

                    Case SyntaxKind.ArrayType
                        Dim oldArrayType = DirectCast(oldType, ArrayTypeSyntax)
                        Dim newArrayType = DirectCast(newType, ArrayTypeSyntax)

                        Return oldArrayType.RankSpecifiers.Count = newArrayType.RankSpecifiers.Count AndAlso
                               CompareTypeNames(oldArrayType.ElementType, newArrayType.ElementType)

                    Case SyntaxKind.NullableType
                        Dim oldNullableType = DirectCast(oldType, NullableTypeSyntax)
                        Dim newNullableType = DirectCast(newType, NullableTypeSyntax)

                        Return CompareTypeNames(oldNullableType.ElementType, newNullableType.ElementType)

                    Case SyntaxKind.IdentifierName,
                         SyntaxKind.QualifiedName,
                         SyntaxKind.GlobalName,
                         SyntaxKind.GenericName
                        Dim oldName = DirectCast(oldType, NameSyntax)
                        Dim newName = DirectCast(newType, NameSyntax)

                        Return CompareNames(oldName, newName)
                End Select

                Debug.Fail(String.Format("Unknown kind: {0}", oldType.Kind))
                Return False
            End Function

            Private Function CompareNames(oldName As NameSyntax, newName As NameSyntax) As Boolean
                If oldName.Kind <> newName.Kind Then
                    Return False
                End If

                Select Case oldName.Kind
                    Case SyntaxKind.IdentifierName
                        Dim oldIdentifierName = DirectCast(oldName, IdentifierNameSyntax)
                        Dim newIdentifierName = DirectCast(newName, IdentifierNameSyntax)

                        Return StringComparer.OrdinalIgnoreCase.Equals(oldIdentifierName.Identifier.ToString(), newIdentifierName.Identifier.ToString())

                    Case SyntaxKind.QualifiedName
                        Dim oldQualifiedName = DirectCast(oldName, QualifiedNameSyntax)
                        Dim newQualifiedName = DirectCast(newName, QualifiedNameSyntax)

                        Return CompareNames(oldQualifiedName.Left, newQualifiedName.Left) AndAlso
                               CompareNames(oldQualifiedName.Right, newQualifiedName.Right)

                    Case SyntaxKind.GenericName
                        Dim oldGenericName = DirectCast(oldName, GenericNameSyntax)
                        Dim newGenericName = DirectCast(newName, GenericNameSyntax)

                        If Not StringComparer.OrdinalIgnoreCase.Equals(oldGenericName.Identifier.ToString(), newGenericName.Identifier.ToString()) Then
                            Return False
                        End If

                        If oldGenericName.Arity <> newGenericName.Arity Then
                            Return False
                        End If

                        For i = 0 To oldGenericName.Arity - 1
                            If Not CompareTypeNames(oldGenericName.TypeArgumentList.Arguments(i), newGenericName.TypeArgumentList.Arguments(i)) Then
                                Return False
                            End If
                        Next

                        Return True

                    Case SyntaxKind.GlobalName
                        Return True
                End Select

                Debug.Fail(String.Format("Unknown kind: {0}", oldName.Kind))
                Return False
            End Function

            Protected Overrides Sub CollectCore(oldRoot As SyntaxNode, newRoot As SyntaxNode, eventQueue As CodeModelEventQueue)
                CompareCompilationUnits(DirectCast(oldRoot, CompilationUnitSyntax), DirectCast(newRoot, CompilationUnitSyntax), eventQueue)
            End Sub

            Private Function GetValidParentNode(node As SyntaxNode) As SyntaxNode
                If TypeOf node Is TypeStatementSyntax AndAlso
                   TypeOf node.Parent Is TypeBlockSyntax Then

                    Return node.Parent
                End If

                If TypeOf node Is EnumStatementSyntax AndAlso
                   TypeOf node.Parent Is EnumBlockSyntax Then

                    Return node.Parent
                End If

                If TypeOf node Is MethodBaseSyntax AndAlso
                   TypeOf node.Parent Is MethodBlockBaseSyntax Then

                    Return node.Parent
                End If

                If TypeOf node Is PropertyStatementSyntax AndAlso
                   TypeOf node.Parent Is PropertyBlockSyntax Then

                    Return node.Parent
                End If

                If TypeOf node Is EventStatementSyntax AndAlso
                   TypeOf node.Parent Is EventBlockSyntax Then

                    Return node.Parent
                End If

                Return node
            End Function

            Protected Overrides Sub EnqueueAddEvent(node As SyntaxNode, parent As SyntaxNode, eventQueue As CodeModelEventQueue)
                If eventQueue Is Nothing Then
                    Return
                End If

                If TypeOf node Is IncompleteMemberSyntax Then
                    Return
                End If

                If TypeOf node Is FieldDeclarationSyntax Then
                    For Each variableDeclarator In DirectCast(node, FieldDeclarationSyntax).Declarators
                        For Each name In variableDeclarator.Names
                            eventQueue.EnqueueAddEvent(name, parent)
                        Next
                    Next

                    Return
                End If

                If TypeOf parent Is FieldDeclarationSyntax Then
                    For Each variableDeclarator In DirectCast(parent, FieldDeclarationSyntax).Declarators
                        For Each name In variableDeclarator.Names
                            eventQueue.EnqueueAddEvent(node, name)
                        Next
                    Next

                    Return
                End If

                eventQueue.EnqueueAddEvent(GetValidParentNode(node), parent)
            End Sub

            Protected Overrides Sub EnqueueChangeEvent(node As SyntaxNode, parent As SyntaxNode, eventType As CodeModelEventType, eventQueue As CodeModelEventQueue)
                If eventQueue Is Nothing Then
                    Return
                End If

                If TypeOf node Is IncompleteMemberSyntax Then
                    Return
                End If

                If TypeOf node Is FieldDeclarationSyntax Then
                    For Each variableDeclarator In DirectCast(node, FieldDeclarationSyntax).Declarators
                        For Each name In variableDeclarator.Names
                            eventQueue.EnqueueChangeEvent(name, parent, eventType)
                        Next
                    Next

                    Return
                End If

                If TypeOf parent Is FieldDeclarationSyntax Then
                    For Each variableDeclarator In DirectCast(parent, FieldDeclarationSyntax).Declarators
                        For Each name In variableDeclarator.Names
                            eventQueue.EnqueueChangeEvent(node, name, eventType)
                        Next
                    Next

                    Return
                End If

                eventQueue.EnqueueChangeEvent(GetValidParentNode(node), parent, eventType)
            End Sub

            Protected Overrides Sub EnqueueRemoveEvent(node As SyntaxNode, parent As SyntaxNode, eventQueue As CodeModelEventQueue)
                If eventQueue Is Nothing Then
                    Return
                End If

                If TypeOf node Is IncompleteMemberSyntax Then
                    Return
                End If

                If TypeOf node Is FieldDeclarationSyntax Then
                    For Each variableDeclarator In DirectCast(node, FieldDeclarationSyntax).Declarators
                        For Each name In variableDeclarator.Names
                            eventQueue.EnqueueRemoveEvent(name, parent)
                        Next
                    Next

                    Return
                End If

                If TypeOf parent Is FieldDeclarationSyntax Then
                    For Each variableDeclarator In DirectCast(parent, FieldDeclarationSyntax).Declarators
                        For Each name In variableDeclarator.Names
                            eventQueue.EnqueueRemoveEvent(node, name)
                        Next
                    Next

                    Return
                End If

                eventQueue.EnqueueRemoveEvent(GetValidParentNode(node), GetValidParentNode(parent))
            End Sub

        End Class
    End Class
End Namespace
