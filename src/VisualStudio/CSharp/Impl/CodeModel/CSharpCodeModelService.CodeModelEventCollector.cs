// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

// Taken from csharp\LanguageAnalysis\Compiler\IDE\LIB\CMEvents.cpp

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal partial class CSharpCodeModelService
    {
        protected override AbstractCodeModelEventCollector CreateCodeModelEventCollector()
            => new CodeModelEventCollector(this);

        private class CodeModelEventCollector : AbstractCodeModelEventCollector
        {
            public CodeModelEventCollector(AbstractCodeModelService codeModelService)
                : base(codeModelService)
            {
            }

            private static IReadOnlyList<MemberDeclarationSyntax> GetValidMembers(SyntaxNode node)
            {
                return CSharpCodeModelService
                    .GetChildMemberNodes(node)
                    .Where(n => !n.IsKind(SyntaxKind.IncompleteMember))
                    .ToArray();
            }

            private void CompareCompilationUnits(
                CompilationUnitSyntax oldCompilationUnit,
                CompilationUnitSyntax newCompilationUnit,
                CodeModelEventQueue eventQueue)
            {
                // Note: In the C# legacy code model, events are generated for the top-level
                // namespace that is at the root of every parse tree. In the Roslyn C# code model
                // implementation, we won't bother.

                CompareChildren(
                    CompareNamespacesOrTypes,
                    GetValidMembers(oldCompilationUnit),
                    GetValidMembers(newCompilationUnit),
                    (SyntaxNode)null,
                    CodeModelEventType.Unknown,
                    eventQueue);
            }

            private bool CompareAttributeLists(
                AttributeListSyntax oldAttributeList,
                AttributeListSyntax newAttributeList,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                return CompareChildren(
                    CompareAttributes,
                    oldAttributeList.Attributes.AsReadOnlyList(),
                    newAttributeList.Attributes.AsReadOnlyList(),
                    newNodeParent,
                    CodeModelEventType.Unknown,
                    eventQueue);
            }

            private bool CompareAttributes(
                AttributeSyntax oldAttribute,
                AttributeSyntax newAttribute,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                Debug.Assert(oldAttribute != null && newAttribute != null);

                var same = true;

                if (!CompareNames(oldAttribute.Name, newAttribute.Name))
                {
                    EnqueueChangeEvent(newAttribute, newNodeParent, CodeModelEventType.Rename, eventQueue);
                    same = false;
                }

                // If arguments have changed enqueue a element changed (arguments changed) node
                if (!CompareAttributeArguments(oldAttribute.ArgumentList, newAttribute.ArgumentList))
                {
                    EnqueueChangeEvent(newAttribute, newNodeParent, CodeModelEventType.ArgChange, eventQueue);
                    same = false;
                }

                return same;
            }

            private bool CompareAttributeArguments(AttributeArgumentListSyntax oldAttributeArguments, AttributeArgumentListSyntax newAttributeArguments)
            {
                if (oldAttributeArguments == null || newAttributeArguments == null)
                {
                    return oldAttributeArguments == newAttributeArguments;
                }

                var oldArguments = oldAttributeArguments.Arguments;
                var newArguments = newAttributeArguments.Arguments;

                if (oldArguments.Count != newArguments.Count)
                {
                    return false;
                }

                for (var i = 0; i < oldArguments.Count; i++)
                {
                    var oldArgument = oldArguments[i];
                    var newArgument = newArguments[i];

                    if (!StringComparer.Ordinal.Equals(CodeModelService.GetName(oldArgument), CodeModelService.GetName(newArgument)))
                    {
                        return false;
                    }

                    if (!CompareExpressions(oldArgument.Expression, newArgument.Expression))
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool CompareExpressions(ExpressionSyntax oldExpression, ExpressionSyntax newExpression)
            {
                if (oldExpression == null || newExpression == null)
                {
                    return oldExpression == newExpression;
                }

                if (oldExpression.Kind() != newExpression.Kind())
                {
                    return false;
                }

                if (oldExpression is TypeSyntax typeSyntax)
                {
                    return CompareTypes(typeSyntax, (TypeSyntax)newExpression);
                }

                if (oldExpression is LiteralExpressionSyntax)
                {
                    return StringComparer.Ordinal.Equals(oldExpression.ToString(), newExpression.ToString());
                }

                if (oldExpression is CastExpressionSyntax oldCast)
                {
                    var newCast = (CastExpressionSyntax)newExpression;

                    return CompareTypes(oldCast.Type, newCast.Type)
                        && CompareExpressions(oldCast.Expression, newCast.Expression);
                }

                if (oldExpression is PrefixUnaryExpressionSyntax prefixUnary)
                {
                    return CompareExpressions(prefixUnary.Operand, ((PrefixUnaryExpressionSyntax)newExpression).Operand);
                }

                if (oldExpression is AwaitExpressionSyntax awaitExpression)
                {
                    return CompareExpressions(awaitExpression.Expression, ((AwaitExpressionSyntax)newExpression).Expression);
                }

                if (oldExpression is PostfixUnaryExpressionSyntax postfixUnary)
                {
                    return CompareExpressions(postfixUnary.Operand, ((PostfixUnaryExpressionSyntax)newExpression).Operand);
                }

                if (oldExpression is BinaryExpressionSyntax oldBinaryExpression)
                {
                    var newBinaryExpression = (BinaryExpressionSyntax)newExpression;

                    return CompareExpressions(oldBinaryExpression.Left, newBinaryExpression.Left)
                        && CompareExpressions(oldBinaryExpression.Right, newBinaryExpression.Right);
                }

                if (oldExpression is AssignmentExpressionSyntax oldAssignmentExpression)
                {
                    var newAssignmentExpression = (AssignmentExpressionSyntax)newExpression;

                    return CompareExpressions(oldAssignmentExpression.Left, newAssignmentExpression.Left)
                        && CompareExpressions(oldAssignmentExpression.Right, newAssignmentExpression.Right);
                }

                if (oldExpression is MemberAccessExpressionSyntax oldMemberAccessExpression)
                {
                    var newMemberAccessExpression = (MemberAccessExpressionSyntax)newExpression;

                    return CompareExpressions(oldMemberAccessExpression.Expression, newMemberAccessExpression.Expression)
                        && CompareExpressions(oldMemberAccessExpression.Name, newMemberAccessExpression.Name);
                }

                return true;
            }

            private bool CompareParameters(ParameterSyntax oldParameter, ParameterSyntax newParameter, SyntaxNode newNodeParent, CodeModelEventQueue eventQueue)
            {
                Debug.Assert(oldParameter != null && newParameter != null);

                var same = true;

                if (!StringComparer.Ordinal.Equals(CodeModelService.GetName(oldParameter), CodeModelService.GetName(newParameter)))
                {
                    EnqueueChangeEvent(newParameter, newNodeParent, CodeModelEventType.Rename, eventQueue);
                    same = false;
                }

                // If modifiers or the type have changed enqueue a element changed (unknown change) node
                if (!CompareModifiers(oldParameter, newParameter) ||
                    !CompareTypes(oldParameter.Type, newParameter.Type))
                {
                    EnqueueChangeEvent(newParameter, newNodeParent, CodeModelEventType.Unknown, eventQueue);
                    same = false;
                }

                return same;
            }

            private bool CompareMemberDeclarations(
                MemberDeclarationSyntax oldMember,
                MemberDeclarationSyntax newMember,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                Debug.Assert(oldMember != null && newMember != null);

                // If the kind doesn't match, it has to be a remove/add.
                if (oldMember.Kind() != newMember.Kind())
                {
                    EnqueueRemoveEvent(oldMember, newNodeParent, eventQueue);
                    EnqueueAddEvent(newMember, newNodeParent, eventQueue);

                    return false;
                }

                if (oldMember is BaseTypeDeclarationSyntax or
                    DelegateDeclarationSyntax)
                {
                    return CompareTypeDeclarations(oldMember, newMember, newNodeParent, eventQueue);
                }
                else if (oldMember is BaseMethodDeclarationSyntax baseMethod)
                {
                    return CompareMethodDeclarations(baseMethod, (BaseMethodDeclarationSyntax)newMember, newNodeParent, eventQueue);
                }
                else if (oldMember is BaseFieldDeclarationSyntax baseField)
                {
                    return CompareFieldDeclarations(baseField, (BaseFieldDeclarationSyntax)newMember, newNodeParent, eventQueue);
                }
                else if (oldMember is BasePropertyDeclarationSyntax baseProperty)
                {
                    return ComparePropertyDeclarations(baseProperty, (BasePropertyDeclarationSyntax)newMember, newNodeParent, eventQueue);
                }
                else if (oldMember is EnumMemberDeclarationSyntax enumMember)
                {
                    return CompareEnumMemberDeclarations(enumMember, (EnumMemberDeclarationSyntax)newMember, newNodeParent, eventQueue);
                }

                throw new NotImplementedException();
            }

            private bool CompareEnumMemberDeclarations(
                EnumMemberDeclarationSyntax oldEnumMember,
                EnumMemberDeclarationSyntax newEnumMember,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                Debug.Assert(oldEnumMember != null && newEnumMember != null);

                var same = true;

                if (!StringComparer.Ordinal.Equals(CodeModelService.GetName(oldEnumMember), CodeModelService.GetName(newEnumMember)))
                {
                    EnqueueChangeEvent(newEnumMember, newNodeParent, CodeModelEventType.Rename, eventQueue);
                    same = false;
                }

                same &= CompareChildren(
                    CompareAttributeLists,
                    oldEnumMember.AttributeLists.AsReadOnlyList(),
                    newEnumMember.AttributeLists.AsReadOnlyList(),
                    newEnumMember,
                    CodeModelEventType.Unknown,
                    eventQueue);

                return same;
            }

            private bool ComparePropertyDeclarations(
                BasePropertyDeclarationSyntax oldProperty,
                BasePropertyDeclarationSyntax newProperty,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                Debug.Assert(oldProperty != null && newProperty != null);

                var same = true;

                if (!StringComparer.Ordinal.Equals(CodeModelService.GetName(oldProperty), CodeModelService.GetName(newProperty)))
                {
                    EnqueueChangeEvent(newProperty, newNodeParent, CodeModelEventType.Rename, eventQueue);
                    same = false;
                }

                // If modifiers have changed enqueue a element changed (unknown change) node
                if (!CompareModifiers(oldProperty, newProperty))
                {
                    EnqueueChangeEvent(newProperty, newNodeParent, CodeModelEventType.Unknown, eventQueue);
                    same = false;
                }

                // If return type had changed enqueue a element changed (typeref changed) node
                if (!CompareTypes(oldProperty.Type, newProperty.Type))
                {
                    EnqueueChangeEvent(newProperty, newNodeParent, CodeModelEventType.TypeRefChange, eventQueue);
                    same = false;
                }

                same &= CompareChildren(
                    CompareAttributeLists,
                    oldProperty.AttributeLists.AsReadOnlyList(),
                    newProperty.AttributeLists.AsReadOnlyList(),
                    newProperty,
                    CodeModelEventType.Unknown,
                    eventQueue);

                if (oldProperty is IndexerDeclarationSyntax oldIndexer)
                {
                    var newIndexer = (IndexerDeclarationSyntax)newProperty;
                    same &= CompareChildren(
                        CompareParameters,
                        oldIndexer.ParameterList.Parameters.AsReadOnlyList(),
                        newIndexer.ParameterList.Parameters.AsReadOnlyList(),
                        newIndexer,
                        CodeModelEventType.SigChange,
                        eventQueue);
                }

                return same;
            }

            private bool CompareVariableDeclarators(
                VariableDeclaratorSyntax oldVariableDeclarator,
                VariableDeclaratorSyntax newVariableDeclarator,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                Debug.Assert(oldVariableDeclarator != null && newVariableDeclarator != null);

                if (!StringComparer.Ordinal.Equals(CodeModelService.GetName(oldVariableDeclarator), CodeModelService.GetName(newVariableDeclarator)))
                {
                    EnqueueChangeEvent(newVariableDeclarator, newNodeParent, CodeModelEventType.Rename, eventQueue);
                    return false;
                }

                return true;
            }

            private bool CompareFieldDeclarations(
                BaseFieldDeclarationSyntax oldField,
                BaseFieldDeclarationSyntax newField,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                Debug.Assert(oldField != null && newField != null);

                var same = true;
                same &= CompareChildren(
                    CompareVariableDeclarators,
                    oldField.Declaration.Variables.AsReadOnlyList(),
                    newField.Declaration.Variables.AsReadOnlyList(),
                    newNodeParent,
                    CodeModelEventType.Unknown,
                    eventQueue);

                // If modifiers have changed enqueue a element changed (unknown change) node
                if (oldField.Kind() != newField.Kind() ||
                    !CompareModifiers(oldField, newField))
                {
                    EnqueueChangeEvent(newField, newNodeParent, CodeModelEventType.Unknown, eventQueue);
                    same = false;
                }

                // If type had changed enqueue a element changed (typeref changed) node
                if (!CompareTypes(oldField.Declaration.Type, newField.Declaration.Type))
                {
                    EnqueueChangeEvent(newField, newNodeParent, CodeModelEventType.TypeRefChange, eventQueue);
                    same = false;
                }

                same &= CompareChildren(
                    CompareAttributeLists,
                    oldField.AttributeLists.AsReadOnlyList(),
                    newField.AttributeLists.AsReadOnlyList(),
                    newField,
                    CodeModelEventType.Unknown, eventQueue);

                return same;
            }

            private bool CompareMethodDeclarations(
                BaseMethodDeclarationSyntax oldMethod,
                BaseMethodDeclarationSyntax newMethod,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                Debug.Assert(oldMethod != null && newMethod != null);

                if (!StringComparer.Ordinal.Equals(CodeModelService.GetName(oldMethod), CodeModelService.GetName(newMethod)))
                {
                    var change = CompareRenamedDeclarations(
                        CompareParameters,
                        oldMethod.ParameterList.Parameters.AsReadOnlyList(),
                        newMethod.ParameterList.Parameters.AsReadOnlyList(),
                        oldMethod,
                        newMethod,
                        newNodeParent,
                        eventQueue);

                    if (change == DeclarationChange.NameOnly)
                    {
                        EnqueueChangeEvent(newMethod, newNodeParent, CodeModelEventType.Rename, eventQueue);
                    }

                    return false;
                }
                else
                {
                    var same = true;

                    if (!CompareModifiers(oldMethod, newMethod))
                    {
                        same = false;
                        EnqueueChangeEvent(newMethod, newNodeParent, CodeModelEventType.Unknown, eventQueue);
                    }

                    if (!CompareTypes(GetReturnType(oldMethod), GetReturnType(newMethod)))
                    {
                        same = false;
                        EnqueueChangeEvent(newMethod, newNodeParent, CodeModelEventType.TypeRefChange, eventQueue);
                    }

                    same &= CompareChildren(
                        CompareAttributeLists,
                        oldMethod.AttributeLists.AsReadOnlyList(),
                        newMethod.AttributeLists.AsReadOnlyList(),
                        newMethod,
                        CodeModelEventType.Unknown,
                        eventQueue);

                    same &= CompareChildren(
                        CompareParameters,
                        oldMethod.ParameterList.Parameters.AsReadOnlyList(),
                        newMethod.ParameterList.Parameters.AsReadOnlyList(),
                        newMethod,
                        CodeModelEventType.SigChange,
                        eventQueue);

                    return same;
                }
            }

            private bool CompareNamespaceDeclarations(
                BaseNamespaceDeclarationSyntax oldNamespace,
                BaseNamespaceDeclarationSyntax newNamespace,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                Debug.Assert(oldNamespace != null && newNamespace != null);

                // Check if the namespace nodes are identical w.r.t Name
                if (!CompareNames(oldNamespace.Name, newNamespace.Name))
                {
                    var change = CompareRenamedDeclarations(
                        CompareNamespacesOrTypes,
                        GetValidMembers(oldNamespace),
                        GetValidMembers(newNamespace),
                        oldNamespace,
                        newNamespace,
                        newNodeParent,
                        eventQueue);

                    if (change == DeclarationChange.NameOnly)
                    {
                        EnqueueChangeEvent(newNamespace, newNodeParent, CodeModelEventType.Rename, eventQueue);
                    }

                    return false;
                }

                return CompareChildren(
                    CompareNamespacesOrTypes,
                    GetValidMembers(oldNamespace),
                    GetValidMembers(newNamespace),
                    newNamespace,
                    CodeModelEventType.Unknown,
                    eventQueue);
            }

            private bool CompareTypeDeclarations(
                MemberDeclarationSyntax oldMember,
                MemberDeclarationSyntax newMember,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                Debug.Assert(oldMember != null && newMember != null);
                Debug.Assert(oldMember is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax);
                Debug.Assert(newMember is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax);

                // If the kind doesn't match, it has to be a remove/add.
                if (oldMember.Kind() != newMember.Kind())
                {
                    EnqueueRemoveEvent(oldMember, newNodeParent, eventQueue);
                    EnqueueAddEvent(newMember, newNodeParent, eventQueue);

                    return false;
                }

                if (oldMember is BaseTypeDeclarationSyntax oldType)
                {
                    var newType = (BaseTypeDeclarationSyntax)newMember;

                    var oldMembers = GetValidMembers(oldType);
                    var newMembers = GetValidMembers(newType);

                    var same = true;

                    // If the type name is different, it might mean that the whole type has been removed and a new one added.
                    // In that case, we shouldn't do any other checks and instead return immediately.
                    if (!StringComparer.Ordinal.Equals(oldType.Identifier.ToString(), newType.Identifier.ToString()))
                    {
                        var change = CompareRenamedDeclarations(
                            CompareMemberDeclarations,
                            oldMembers,
                            newMembers,
                            oldType,
                            newType,
                            newNodeParent,
                            eventQueue);

                        if (change == DeclarationChange.WholeDeclaration)
                        {
                            return false;
                        }

                        same = false;
                        EnqueueChangeEvent(newType, newNodeParent, CodeModelEventType.Rename, eventQueue);
                    }

                    if (!CompareModifiers(oldType, newType))
                    {
                        same = false;
                        EnqueueChangeEvent(newType, newNodeParent, CodeModelEventType.Unknown, eventQueue);
                    }

                    if (!CompareBaseLists(oldType, newType))
                    {
                        same = false;
                        EnqueueChangeEvent(newType, newNodeParent, CodeModelEventType.BaseChange, eventQueue);
                    }

                    same &= CompareChildren(
                        CompareAttributeLists,
                        oldType.AttributeLists.AsReadOnlyList(),
                        newType.AttributeLists.AsReadOnlyList(),
                        newType,
                        CodeModelEventType.Unknown,
                        eventQueue);

                    same &= CompareChildren(
                        CompareMemberDeclarations,
                        oldMembers,
                        newMembers,
                        newType,
                        CodeModelEventType.Unknown,
                        eventQueue);

                    return same;
                }
                else if (oldMember is DelegateDeclarationSyntax oldDelegate)
                {
                    var newDelegate = (DelegateDeclarationSyntax)newMember;

                    var same = true;

                    // If the delegate name is different, it might mean that the whole delegate has been removed and a new one added.
                    // In that case, we shouldn't do any other checks and instead return immediately.
                    if (!StringComparer.Ordinal.Equals(oldDelegate.Identifier.ToString(), newDelegate.Identifier.ToString()))
                    {
                        var change = CompareRenamedDeclarations(
                            CompareParameters,
                            oldDelegate.ParameterList.Parameters.AsReadOnlyList(),
                            newDelegate.ParameterList.Parameters.AsReadOnlyList(),
                            oldDelegate,
                            newDelegate,
                            newNodeParent,
                            eventQueue);

                        if (change == DeclarationChange.WholeDeclaration)
                        {
                            return false;
                        }

                        same = false;
                        EnqueueChangeEvent(newDelegate, newNodeParent, CodeModelEventType.Rename, eventQueue);
                    }

                    if (!CompareModifiers(oldDelegate, newDelegate))
                    {
                        same = false;
                        EnqueueChangeEvent(newDelegate, newNodeParent, CodeModelEventType.Unknown, eventQueue);
                    }

                    if (!CompareTypes(oldDelegate.ReturnType, newDelegate.ReturnType))
                    {
                        same = false;
                        EnqueueChangeEvent(newDelegate, newNodeParent, CodeModelEventType.TypeRefChange, eventQueue);
                    }

                    same &= CompareChildren(
                        CompareAttributeLists,
                        oldDelegate.AttributeLists.AsReadOnlyList(),
                        newDelegate.AttributeLists.AsReadOnlyList(),
                        newDelegate,
                        CodeModelEventType.Unknown,
                        eventQueue);

                    same &= CompareChildren(
                        CompareParameters,
                        oldDelegate.ParameterList.Parameters.AsReadOnlyList(),
                        newDelegate.ParameterList.Parameters.AsReadOnlyList(),
                        newDelegate,
                        CodeModelEventType.SigChange,
                        eventQueue);

                    return same;
                }

                return false;
            }

            private bool CompareNamespacesOrTypes(
                MemberDeclarationSyntax oldNamespaceOrType,
                MemberDeclarationSyntax newNamespaceOrType,
                SyntaxNode newNodeParent,
                CodeModelEventQueue eventQueue)
            {
                // If the kind doesn't match, it has to be a remove/add.
                if (oldNamespaceOrType.Kind() != newNamespaceOrType.Kind())
                {
                    EnqueueRemoveEvent(oldNamespaceOrType, newNodeParent, eventQueue);
                    EnqueueAddEvent(newNamespaceOrType, newNodeParent, eventQueue);

                    return false;
                }

                if (oldNamespaceOrType is BaseTypeDeclarationSyntax or
                    DelegateDeclarationSyntax)
                {
                    return CompareTypeDeclarations(oldNamespaceOrType, newNamespaceOrType, newNodeParent, eventQueue);
                }
                else if (oldNamespaceOrType is BaseNamespaceDeclarationSyntax namespaceDecl)
                {
                    return CompareNamespaceDeclarations(namespaceDecl, (BaseNamespaceDeclarationSyntax)newNamespaceOrType, newNodeParent, eventQueue);
                }

                return false;
            }

            private bool CompareBaseLists(BaseTypeDeclarationSyntax oldType, BaseTypeDeclarationSyntax newType)
            {
                if (oldType.BaseList == null && newType.BaseList == null)
                {
                    return true;
                }

                if (oldType.BaseList != null && newType.BaseList != null)
                {
                    var oldTypes = oldType.BaseList.Types;
                    var newTypes = newType.BaseList.Types;

                    if (oldTypes.Count != newTypes.Count)
                    {
                        return false;
                    }

                    for (var i = 0; i < oldTypes.Count; i++)
                    {
                        if (!CompareTypes(oldTypes[i].Type, newTypes[i].Type))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                // In this case, one of the base lists is null.
                return false;
            }

            private static bool CompareModifiers(MemberDeclarationSyntax oldMember, MemberDeclarationSyntax newMember)
                => oldMember.GetModifierFlags() == newMember.GetModifierFlags();

            private static bool CompareModifiers(ParameterSyntax oldParameter, ParameterSyntax newParameter)
                => oldParameter.GetParameterFlags() == newParameter.GetParameterFlags();

            private bool CompareNames(NameSyntax oldName, NameSyntax newName)
            {
                if (oldName.Kind() != newName.Kind())
                {
                    return false;
                }

                switch (oldName.Kind())
                {
                    case SyntaxKind.IdentifierName:
                        var oldIdentifierName = (IdentifierNameSyntax)oldName;
                        var newIdentifierName = (IdentifierNameSyntax)newName;

                        return StringComparer.Ordinal.Equals(oldIdentifierName.Identifier.ToString(), newIdentifierName.Identifier.ToString());

                    case SyntaxKind.QualifiedName:
                        var oldQualifiedName = (QualifiedNameSyntax)oldName;
                        var newQualifiedName = (QualifiedNameSyntax)newName;

                        return CompareNames(oldQualifiedName.Left, newQualifiedName.Left)
                            && CompareNames(oldQualifiedName.Right, oldQualifiedName.Right);

                    case SyntaxKind.GenericName:
                        var oldGenericName = (GenericNameSyntax)oldName;
                        var newGenericName = (GenericNameSyntax)newName;

                        if (!StringComparer.Ordinal.Equals(oldGenericName.Identifier.ToString(), newGenericName.Identifier.ToString()))
                        {
                            return false;
                        }

                        if (oldGenericName.Arity != newGenericName.Arity)
                        {
                            return false;
                        }

                        for (var i = 0; i < oldGenericName.Arity; i++)
                        {
                            if (!CompareTypes(oldGenericName.TypeArgumentList.Arguments[i], newGenericName.TypeArgumentList.Arguments[i]))
                            {
                                return false;
                            }
                        }

                        return true;

                    case SyntaxKind.AliasQualifiedName:
                        var oldAliasQualifiedName = (AliasQualifiedNameSyntax)oldName;
                        var newAliasQualifiedName = (AliasQualifiedNameSyntax)newName;

                        return CompareNames(oldAliasQualifiedName.Alias, newAliasQualifiedName.Alias)
                            && CompareNames(oldAliasQualifiedName.Name, newAliasQualifiedName.Name);
                }

                Debug.Fail("Unknown kind: " + oldName.Kind());
                return false;
            }

            private bool CompareTypes(TypeSyntax oldType, TypeSyntax newType)
            {
                // Type nodes can be NULL for ctor/dtor/operators ...
                if (oldType == null || newType == null)
                {
                    return oldType == newType;
                }

                if (oldType.Kind() != newType.Kind())
                {
                    return false;
                }

                switch (oldType.Kind())
                {
                    case SyntaxKind.PredefinedType:
                        var oldPredefinedType = (PredefinedTypeSyntax)oldType;
                        var newPredefinedType = (PredefinedTypeSyntax)newType;

                        return oldPredefinedType.Keyword.RawKind == newPredefinedType.Keyword.RawKind;

                    case SyntaxKind.ArrayType:
                        var oldArrayType = (ArrayTypeSyntax)oldType;
                        var newArrayType = (ArrayTypeSyntax)newType;

                        return oldArrayType.RankSpecifiers.Count == newArrayType.RankSpecifiers.Count
                            && CompareTypes(oldArrayType.ElementType, newArrayType.ElementType);

                    case SyntaxKind.PointerType:
                        var oldPointerType = (PointerTypeSyntax)oldType;
                        var newPointerType = (PointerTypeSyntax)newType;

                        return CompareTypes(oldPointerType.ElementType, newPointerType.ElementType);

                    case SyntaxKind.NullableType:
                        var oldNullableType = (NullableTypeSyntax)oldType;
                        var newNullableType = (NullableTypeSyntax)newType;

                        return CompareTypes(oldNullableType.ElementType, newNullableType.ElementType);

                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.QualifiedName:
                    case SyntaxKind.AliasQualifiedName:
                    case SyntaxKind.GenericName:
                        var oldName = (NameSyntax)oldType;
                        var newName = (NameSyntax)newType;

                        return CompareNames(oldName, newName);
                }

                Debug.Fail("Unknown kind: " + oldType.Kind());
                return false;
            }

            private static TypeSyntax GetReturnType(BaseMethodDeclarationSyntax method)
            {
                if (method is MethodDeclarationSyntax methodDecl)
                {
                    return methodDecl.ReturnType;
                }
                else if (method is OperatorDeclarationSyntax operatorDecl)
                {
                    return operatorDecl.ReturnType;
                }

                // TODO(DustinCa): What about conversion operators? How does the legacy code base handle those?

                return null;
            }

            protected override void CollectCore(SyntaxNode oldRoot, SyntaxNode newRoot, CodeModelEventQueue eventQueue)
                => CompareCompilationUnits((CompilationUnitSyntax)oldRoot, (CompilationUnitSyntax)newRoot, eventQueue);

            protected override void EnqueueAddEvent(SyntaxNode node, SyntaxNode parent, CodeModelEventQueue eventQueue)
            {
                if (eventQueue == null)
                {
                    return;
                }

                if (node is IncompleteMemberSyntax)
                {
                    return;
                }

                if (node is BaseFieldDeclarationSyntax baseField)
                {
                    foreach (var variableDeclarator in baseField.Declaration.Variables)
                    {
                        eventQueue.EnqueueAddEvent(variableDeclarator, parent);
                    }
                }
                else if (node is AttributeListSyntax attributeList)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        AddEventToEventQueueForAttributes(attribute, parent, eventQueue.EnqueueAddEvent);
                    }
                }
                else if (node is AttributeSyntax attribute)
                {
                    AddEventToEventQueueForAttributes(attribute, parent, eventQueue.EnqueueAddEvent);
                }
                else
                {
                    eventQueue.EnqueueAddEvent(node, parent);
                }
            }

            protected override void EnqueueChangeEvent(SyntaxNode node, SyntaxNode parent, CodeModelEventType eventType, CodeModelEventQueue eventQueue)
            {
                if (eventQueue == null)
                {
                    return;
                }

                if (node is IncompleteMemberSyntax)
                {
                    return;
                }

                if (node is BaseFieldDeclarationSyntax baseField)
                {
                    foreach (var variableDeclarator in baseField.Declaration.Variables)
                    {
                        eventQueue.EnqueueChangeEvent(variableDeclarator, parent, eventType);
                    }
                }
                else if (node is AttributeListSyntax attributeList)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        ChangeEventQueueForAttributes(attribute, parent, eventType, eventQueue);
                    }
                }
                else if (node is AttributeSyntax attribute)
                {
                    ChangeEventQueueForAttributes(attribute, parent, eventType, eventQueue);
                }
                else
                {
                    eventQueue.EnqueueChangeEvent(node, parent, eventType);
                }
            }

            private static void ChangeEventQueueForAttributes(AttributeSyntax attribute, SyntaxNode parent, CodeModelEventType eventType, CodeModelEventQueue eventQueue)
            {
                if (parent is BaseFieldDeclarationSyntax baseField)
                {
                    foreach (var variableDeclarator in baseField.Declaration.Variables)
                    {
                        eventQueue.EnqueueChangeEvent(attribute, variableDeclarator, eventType);
                    }
                }
                else
                {
                    eventQueue.EnqueueChangeEvent(attribute, parent, eventType);
                }
            }

            protected override void EnqueueRemoveEvent(SyntaxNode node, SyntaxNode parent, CodeModelEventQueue eventQueue)
            {
                if (eventQueue == null)
                {
                    return;
                }

                if (node is IncompleteMemberSyntax)
                {
                    return;
                }

                if (node is BaseFieldDeclarationSyntax baseField)
                {
                    foreach (var variableDeclarator in baseField.Declaration.Variables)
                    {
                        eventQueue.EnqueueRemoveEvent(variableDeclarator, parent);
                    }
                }
                else if (node is AttributeListSyntax attributeList)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        AddEventToEventQueueForAttributes(attribute, parent, eventQueue.EnqueueRemoveEvent);
                    }
                }
                else if (node is AttributeSyntax attribute)
                {
                    AddEventToEventQueueForAttributes(attribute, parent, eventQueue.EnqueueRemoveEvent);
                }
                else
                {
                    eventQueue.EnqueueRemoveEvent(node, parent);
                }
            }

            private static void AddEventToEventQueueForAttributes(AttributeSyntax attribute, SyntaxNode parent, Action<SyntaxNode, SyntaxNode> enqueueAddOrRemoveEvent)
            {
                if (parent is BaseFieldDeclarationSyntax baseField)
                {
                    foreach (var variableDeclarator in baseField.Declaration.Variables)
                    {
                        enqueueAddOrRemoveEvent(attribute, variableDeclarator);
                    }
                }
                else
                {
                    enqueueAddOrRemoveEvent(attribute, parent);
                }
            }
        }
    }
}
