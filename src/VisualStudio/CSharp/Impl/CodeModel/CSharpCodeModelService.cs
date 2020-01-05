// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Extenders;
using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.MethodXml;
using Microsoft.VisualStudio.LanguageServices.CSharp.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal partial class CSharpCodeModelService : AbstractCodeModelService
    {
        internal CSharpCodeModelService(
            HostLanguageServices languageServiceProvider,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IEnumerable<IRefactorNotifyService> refactorNotifyServices)
            : base(languageServiceProvider,
                   editorOptionsFactoryService,
                   refactorNotifyServices,
                   BlankLineInGeneratedMethodFormattingRule.Instance,
                   EndRegionFormattingRule.Instance)
        {
        }

        private static readonly SymbolDisplayFormat s_codeTypeRefAsFullNameFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.ExpandNullable);

        private static readonly SymbolDisplayFormat s_codeTypeRefAsStringFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat s_externalNameFormat =
            new SymbolDisplayFormat(
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                parameterOptions: SymbolDisplayParameterOptions.IncludeName);

        private static readonly SymbolDisplayFormat s_externalFullNameFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeExplicitInterface,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers,
                parameterOptions: SymbolDisplayParameterOptions.IncludeName);

        private static readonly SymbolDisplayFormat s_setTypeFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static bool IsNameableNode(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.EnumMemberDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.StructDeclaration:
                    return true;

                case SyntaxKind.VariableDeclarator:
                    // Could be a regular field or an event field.
                    return node.FirstAncestorOrSelf<BaseFieldDeclarationSyntax>() != null;

                default:
                    return false;
            }
        }

        public override EnvDTE.vsCMElement GetElementKind(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return EnvDTE.vsCMElement.vsCMElementClass;

                default:
                    Debug.Fail("Unsupported element kind: " + node.Kind());
                    throw Exceptions.ThrowEInvalidArg();
            }
        }

        public override bool MatchesScope(SyntaxNode node, EnvDTE.vsCMElement scope)
        {
            switch (node.Kind())
            {
                case SyntaxKind.NamespaceDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementNamespace &&
                        node.Parent != null)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.ClassDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementClass)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementFunction)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.EnumMemberDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementVariable)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.FieldDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementVariable)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.EventDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementEvent)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementProperty)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.Attribute:
                    if (scope == EnvDTE.vsCMElement.vsCMElementAttribute)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.InterfaceDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementInterface)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.DelegateDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementDelegate)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.EnumDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementEnum)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.StructDeclaration:
                    if (scope == EnvDTE.vsCMElement.vsCMElementStruct)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.UsingDirective:
                    if (scope == EnvDTE.vsCMElement.vsCMElementImportStmt &&
                        ((UsingDirectiveSyntax)node).Name != null)
                    {
                        return true;
                    }

                    break;

                case SyntaxKind.VariableDeclaration:
                case SyntaxKind.VariableDeclarator:
                    // The parent of a VariableDeclarator might be an event or
                    // a field declaration. If the parent matches the desired
                    // scope, then this node matches the scope as well.
                    return MatchesScope(node.Parent, scope);

                case SyntaxKind.Parameter:
                    if (scope == EnvDTE.vsCMElement.vsCMElementParameter)
                    {
                        return true;
                    }

                    break;

                default:
                    return false;
            }

            return false;
        }

        public override IEnumerable<SyntaxNode> GetOptionNodes(SyntaxNode parent)
        {
            // Only VB has Option statements
            return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        public override IEnumerable<SyntaxNode> GetImportNodes(SyntaxNode parent)
            => parent.Kind() switch
            {
                SyntaxKind.CompilationUnit => ((CompilationUnitSyntax)parent).Usings,
                SyntaxKind.NamespaceDeclaration => ((NamespaceDeclarationSyntax)parent).Usings,
                _ => SpecializedCollections.EmptyEnumerable<SyntaxNode>(),
            };

        private static IEnumerable<SyntaxNode> GetAttributeNodes(SyntaxList<AttributeListSyntax> attributeDeclarationList)
        {
            foreach (var attributeDeclaration in attributeDeclarationList)
            {
                foreach (var attribute in attributeDeclaration.Attributes)
                {
                    yield return attribute;
                }
            }
        }

        public override IEnumerable<SyntaxNode> GetAttributeNodes(SyntaxNode parent)
        {
            if (parent is CompilationUnitSyntax compilationUnit)
            {
                return GetAttributeNodes(compilationUnit.AttributeLists);
            }
            else if (parent is BaseTypeDeclarationSyntax baseType)
            {
                return GetAttributeNodes(baseType.AttributeLists);
            }
            else if (parent is BaseMethodDeclarationSyntax baseMethod)
            {
                return GetAttributeNodes(baseMethod.AttributeLists);
            }
            else if (parent is BasePropertyDeclarationSyntax baseProperty)
            {
                return GetAttributeNodes(baseProperty.AttributeLists);
            }
            else if (parent is BaseFieldDeclarationSyntax baseField)
            {
                return GetAttributeNodes(baseField.AttributeLists);
            }
            else if (parent is DelegateDeclarationSyntax delegateDecl)
            {
                return GetAttributeNodes(delegateDecl.AttributeLists);
            }
            else if (parent is EnumMemberDeclarationSyntax enumMember)
            {
                return GetAttributeNodes(enumMember.AttributeLists);
            }
            else if (parent is ParameterSyntax parameter)
            {
                return GetAttributeNodes(parameter.AttributeLists);
            }
            else if (parent is VariableDeclaratorSyntax ||
                     parent is VariableDeclarationSyntax)
            {
                return GetAttributeNodes(parent.Parent);
            }
            else if (parent is AccessorDeclarationSyntax accessor)
            {
                return GetAttributeNodes(accessor.AttributeLists);
            }

            return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        public override IEnumerable<SyntaxNode> GetAttributeArgumentNodes(SyntaxNode parent)
        {
            if (parent is AttributeSyntax attribute)
            {
                if (attribute.ArgumentList == null)
                {
                    return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
                }

                return attribute.ArgumentList.Arguments;
            }

            return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        public override IEnumerable<SyntaxNode> GetInheritsNodes(SyntaxNode parent)
        {
            // Only VB has Inherits statements
            return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        public override IEnumerable<SyntaxNode> GetImplementsNodes(SyntaxNode parent)
        {
            // Only VB has Implements statements
            return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        private static bool IsContainerNode(SyntaxNode container) =>
            container is CompilationUnitSyntax ||
            container is NamespaceDeclarationSyntax ||
            container is BaseTypeDeclarationSyntax;

        private static bool IsNamespaceOrTypeDeclaration(SyntaxNode node) =>
            node.Kind() == SyntaxKind.NamespaceDeclaration ||
            node is BaseTypeDeclarationSyntax ||
            node is DelegateDeclarationSyntax;

        private static IEnumerable<MemberDeclarationSyntax> GetChildMemberNodes(SyntaxNode container)
        {
            if (container is CompilationUnitSyntax compilationUnit)
            {
                foreach (var member in compilationUnit.Members)
                {
                    if (IsNamespaceOrTypeDeclaration(member))
                    {
                        yield return member;
                    }
                }
            }
            else if (container is NamespaceDeclarationSyntax namespaceDecl)
            {
                foreach (var member in namespaceDecl.Members)
                {
                    if (IsNamespaceOrTypeDeclaration(member))
                    {
                        yield return member;
                    }
                }
            }
            else if (container is TypeDeclarationSyntax typeDecl)
            {
                foreach (var member in typeDecl.Members)
                {
                    if (member.Kind() != SyntaxKind.NamespaceDeclaration)
                    {
                        yield return member;
                    }
                }
            }
            else if (container is EnumDeclarationSyntax enumDecl)
            {
                foreach (var member in enumDecl.Members)
                {
                    yield return member;
                }
            }
        }

        private static bool NodeIsSupported(bool test, SyntaxNode node)
        {
            return !test || IsNameableNode(node);
        }

        /// <summary>
        /// Retrieves the members of a specified <paramref name="container"/> node. The members that are
        /// returned can be controlled by passing various parameters.
        /// </summary>
        /// <param name="container">The <see cref="SyntaxNode"/> from which to retrieve members.</param>
        /// <param name="includeSelf">If true, the container is returned as well.</param>
        /// <param name="recursive">If true, members are recursed to return descendant members as well
        /// as immediate children. For example, a namespace would return the namespaces and types within.
        /// However, if <paramref name="recursive"/> is true, members with the namespaces and types would
        /// also be returned.</param>
        /// <param name="logicalFields">If true, field declarations are broken into their respective declarators.
        /// For example, the field "int x, y" would return two declarators, one for x and one for y in place
        /// of the field.</param>
        /// <param name="onlySupportedNodes">If true, only members supported by Code Model are returned.</param>
        public override IEnumerable<SyntaxNode> GetMemberNodes(SyntaxNode container, bool includeSelf, bool recursive, bool logicalFields, bool onlySupportedNodes)
        {
            if (!IsContainerNode(container))
            {
                yield break;
            }

            if (includeSelf && NodeIsSupported(onlySupportedNodes, container))
            {
                yield return container;
            }

            foreach (var member in GetChildMemberNodes(container))
            {
                if (member is BaseFieldDeclarationSyntax baseField)
                {
                    // For fields, the 'logical' and 'supported' flags are intrinsically tied.
                    //   * If 'logical' is true, only declarators should be returned, regardless of the value of 'supported'.
                    //   * If 'logical' is false, the field should only be returned if 'supported' is also false.

                    if (logicalFields)
                    {
                        foreach (var declarator in baseField.Declaration.Variables)
                        {
                            // We know that variable declarators are supported, so there's no need to check them here.
                            yield return declarator;
                        }
                    }
                    else if (!onlySupportedNodes)
                    {
                        // Only return field declarations if the supported flag is false.
                        yield return member;
                    }
                }
                else if (NodeIsSupported(onlySupportedNodes, member))
                {
                    yield return member;
                }

                if (recursive && IsContainerNode(member))
                {
                    foreach (var innerMember in GetMemberNodes(member, includeSelf: false, recursive: true, logicalFields: logicalFields, onlySupportedNodes: onlySupportedNodes))
                    {
                        yield return innerMember;
                    }
                }
            }
        }

        public override string Language
        {
            get { return EnvDTE.CodeModelLanguageConstants.vsCMLanguageCSharp; }
        }

        public override string AssemblyAttributeString
        {
            get
            {
                return "assembly";
            }
        }

        /// <summary>
        /// Do not use this method directly! Instead, go through <see cref="FileCodeModel.GetOrCreateCodeElement{T}(SyntaxNode)"/>
        /// </summary>
        public override EnvDTE.CodeElement CreateInternalCodeElement(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNode node)
        {
            // Attributes, attribute arguments, parameters, imports directives, and
            // accessor functions do not have their own node keys. Rather, they are
            // based off their parents node keys and other data.
            switch (node.Kind())
            {
                case SyntaxKind.Attribute:
                    return (EnvDTE.CodeElement)CreateInternalCodeAttribute(state, fileCodeModel, node);

                case SyntaxKind.AttributeArgument:
                    return (EnvDTE.CodeElement)CreateInternalCodeAttributeArgument(state, fileCodeModel, (AttributeArgumentSyntax)node);

                case SyntaxKind.Parameter:
                    return (EnvDTE.CodeElement)CreateInternalCodeParameter(state, fileCodeModel, (ParameterSyntax)node);

                case SyntaxKind.UsingDirective:
                    return CreateInternalCodeImport(state, fileCodeModel, (UsingDirectiveSyntax)node);
            }

            if (IsAccessorNode(node))
            {
                return (EnvDTE.CodeElement)CreateInternalCodeAccessorFunction(state, fileCodeModel, node);
            }

            var nodeKey = GetNodeKey(node);

            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return (EnvDTE.CodeElement)CodeClass.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                case SyntaxKind.InterfaceDeclaration:
                    return (EnvDTE.CodeElement)CodeInterface.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                case SyntaxKind.StructDeclaration:
                    return (EnvDTE.CodeElement)CodeStruct.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                case SyntaxKind.EnumDeclaration:
                    return (EnvDTE.CodeElement)CodeEnum.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                case SyntaxKind.EnumMemberDeclaration:
                    return (EnvDTE.CodeElement)CodeVariable.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                case SyntaxKind.DelegateDeclaration:
                    return (EnvDTE.CodeElement)CodeDelegate.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    return (EnvDTE.CodeElement)CodeFunction.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                case SyntaxKind.NamespaceDeclaration:
                    return (EnvDTE.CodeElement)CodeNamespace.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    return (EnvDTE.CodeElement)CodeProperty.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                case SyntaxKind.EventDeclaration:
                    return (EnvDTE.CodeElement)CodeEvent.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                case SyntaxKind.VariableDeclarator:
                    var baseFieldDeclaration = node.FirstAncestorOrSelf<BaseFieldDeclarationSyntax>();
                    if (baseFieldDeclaration != null)
                    {
                        if (baseFieldDeclaration.Kind() == SyntaxKind.FieldDeclaration)
                        {
                            return (EnvDTE.CodeElement)CodeVariable.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                        }
                        else if (baseFieldDeclaration.Kind() == SyntaxKind.EventFieldDeclaration)
                        {
                            return (EnvDTE.CodeElement)CodeEvent.Create(state, fileCodeModel, nodeKey, (int)node.Kind());
                        }
                    }

                    throw Exceptions.ThrowEUnexpected();
                default:
                    throw new InvalidOperationException();
            }
        }

        public override EnvDTE.CodeElement CreateUnknownCodeElement(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.NamespaceDeclaration:
                    return (EnvDTE.CodeElement)CodeNamespace.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));

                case SyntaxKind.ClassDeclaration:
                    return (EnvDTE.CodeElement)CodeClass.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));
                case SyntaxKind.InterfaceDeclaration:
                    return (EnvDTE.CodeElement)CodeInterface.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));
                case SyntaxKind.StructDeclaration:
                    return (EnvDTE.CodeElement)CodeStruct.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));
                case SyntaxKind.EnumDeclaration:
                    return (EnvDTE.CodeElement)CodeEnum.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));
                case SyntaxKind.DelegateDeclaration:
                    return (EnvDTE.CodeElement)CodeDelegate.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));

                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    return (EnvDTE.CodeElement)CodeFunction.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));

                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    return (EnvDTE.CodeElement)CodeProperty.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));

                case SyntaxKind.EventDeclaration:
                    return (EnvDTE.CodeElement)CodeEvent.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));

                case SyntaxKind.VariableDeclarator:
                    var eventFieldDeclaration = node.FirstAncestorOrSelf<EventFieldDeclarationSyntax>();
                    if (eventFieldDeclaration != null)
                    {
                        return (EnvDTE.CodeElement)CodeEvent.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));
                    }

                    goto case SyntaxKind.EnumMemberDeclaration;

                case SyntaxKind.EnumMemberDeclaration:
                    return (EnvDTE.CodeElement)CodeVariable.CreateUnknown(state, fileCodeModel, node.RawKind, GetName(node));

                default:
                    throw Exceptions.ThrowEUnexpected();
            }
        }

        public override EnvDTE.CodeElement CreateUnknownRootNamespaceCodeElement(CodeModelState state, FileCodeModel fileCodeModel)
        {
            return (EnvDTE.CodeElement)CodeNamespace.CreateUnknown(state, fileCodeModel, (int)SyntaxKind.NamespaceDeclaration, string.Empty);
        }

        public override EnvDTE.CodeTypeRef CreateCodeTypeRef(CodeModelState state, ProjectId projectId, object type)
        {
            var project = state.Workspace.CurrentSolution.GetProject(projectId);
            if (project == null)
            {
                throw Exceptions.ThrowEFail();
            }

            var compilation = project.GetCompilationAsync().Result;

            if (type is EnvDTE.vsCMTypeRef || type is int)
            {
                var specialType = GetSpecialType((EnvDTE.vsCMTypeRef)type);
                return CodeTypeRef.Create(state, null, projectId, compilation.GetSpecialType(specialType));
            }

            string typeName;
            object parent = null;

            if (type is EnvDTE.CodeType codeType)
            {
                typeName = codeType.FullName;
                parent = codeType;
            }
            else if (type is string str)
            {
                typeName = str;
            }
            else
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            var typeSymbol = GetTypeSymbolFromFullName(typeName, compilation);
            if (typeSymbol == null)
            {
                throw Exceptions.ThrowEFail();
            }

            if (typeSymbol.TypeKind == TypeKind.Unknown)
            {
                if (SyntaxFactsService.IsValidIdentifier(typeSymbol.Name))
                {
                    throw Exceptions.ThrowEFail();
                }
            }

            return CodeTypeRef.Create(state, parent, projectId, typeSymbol);
        }

        public override EnvDTE.vsCMTypeRef GetTypeKindForCodeTypeRef(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind == TypeKind.Array)
            {
                return EnvDTE.vsCMTypeRef.vsCMTypeRefArray;
            }
            else if (typeSymbol.TypeKind == TypeKind.Pointer)
            {
                return EnvDTE.vsCMTypeRef.vsCMTypeRefPointer;
            }
            else if (
                typeSymbol.TypeKind == TypeKind.Dynamic ||
                typeSymbol.TypeKind == TypeKind.Unknown)
            {
                return EnvDTE.vsCMTypeRef.vsCMTypeRefOther;
            }
            else
            {
                switch (typeSymbol.SpecialType)
                {
                    case SpecialType.System_Void:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefVoid;
                    case SpecialType.System_String:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefString;
                    case SpecialType.System_Object:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefObject;
                    case SpecialType.System_Char:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefChar;
                    case SpecialType.System_Byte:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefByte;
                    case SpecialType.System_Boolean:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefBool;
                    case SpecialType.System_Int16:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefShort;
                    case SpecialType.System_Int32:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefInt;
                    case SpecialType.System_Int64:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefLong;
                    case SpecialType.System_Single:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefFloat;
                    case SpecialType.System_Double:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefDouble;
                    case SpecialType.System_Decimal:
                        return EnvDTE.vsCMTypeRef.vsCMTypeRefDecimal;
                    case SpecialType.System_UInt16:
                        return (EnvDTE.vsCMTypeRef)EnvDTE80.vsCMTypeRef2.vsCMTypeRefUnsignedShort;
                    case SpecialType.System_UInt32:
                        return (EnvDTE.vsCMTypeRef)EnvDTE80.vsCMTypeRef2.vsCMTypeRefUnsignedInt;
                    case SpecialType.System_UInt64:
                        return (EnvDTE.vsCMTypeRef)EnvDTE80.vsCMTypeRef2.vsCMTypeRefUnsignedLong;
                    case SpecialType.System_SByte:
                        return (EnvDTE.vsCMTypeRef)EnvDTE80.vsCMTypeRef2.vsCMTypeRefSByte;
                }

                // Comment below is from native code
                // The following are not supported
                // vsCMTypeRefUnsignedChar - PT_UCHAR not in C# ??
                // vsCMTypeRefReference - C++ specific
                // vsCMTypeRefMCBoxedReference - C++ specific
            }

            if (typeSymbol.TypeKind == TypeKind.Class ||
                typeSymbol.TypeKind == TypeKind.Interface ||
                typeSymbol.TypeKind == TypeKind.Enum ||
                typeSymbol.TypeKind == TypeKind.Struct ||
                typeSymbol.TypeKind == TypeKind.Delegate)
            {
                return EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType;
            }

            return EnvDTE.vsCMTypeRef.vsCMTypeRefOther;
        }

        public override string GetAsFullNameForCodeTypeRef(ITypeSymbol typeSymbol)
        {
            return typeSymbol.ToDisplayString(s_codeTypeRefAsFullNameFormat);
        }

        public override string GetAsStringForCodeTypeRef(ITypeSymbol typeSymbol)
        {
            return typeSymbol.ToDisplayString(s_codeTypeRefAsStringFormat);
        }

        public override bool IsParameterNode(SyntaxNode node)
        {
            return node is ParameterSyntax;
        }

        public override bool IsAttributeNode(SyntaxNode node)
        {
            return node is AttributeSyntax;
        }

        public override bool IsAttributeArgumentNode(SyntaxNode node)
        {
            return node is AttributeArgumentSyntax;
        }

        public override bool IsOptionNode(SyntaxNode node)
        {
            // Only VB implementation has Option statements
            return false;
        }

        public override bool IsImportNode(SyntaxNode node)
        {
            return node is UsingDirectiveSyntax;
        }

        public override string GetUnescapedName(string name)
        {
            return name != null && name.Length > 1 && name[0] == '@'
                ? name.Substring(1)
                : name;
        }

        public override string GetName(SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.EnumDeclaration:
                    return ((BaseTypeDeclarationSyntax)node).Identifier.ToString();
                case SyntaxKind.DelegateDeclaration:
                    return ((DelegateDeclarationSyntax)node).Identifier.ToString();
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)node).ExplicitInterfaceSpecifier?.ToString() +
                        ((MethodDeclarationSyntax)node).Identifier.ToString();
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)node).Identifier.ToString();
                case SyntaxKind.DestructorDeclaration:
                    return "~" + ((DestructorDeclarationSyntax)node).Identifier.ToString();
                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)node).ExplicitInterfaceSpecifier?.ToString() +
                        ((PropertyDeclarationSyntax)node).Identifier.ToString();
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)node).ExplicitInterfaceSpecifier?.ToString() +
                        ((IndexerDeclarationSyntax)node).ThisKeyword.ToString();
                case SyntaxKind.EventDeclaration:
                    return ((EventDeclarationSyntax)node).ExplicitInterfaceSpecifier?.ToString() +
                        ((EventDeclarationSyntax)node).Identifier.ToString();
                case SyntaxKind.Parameter:
                    return GetParameterName(node);
                case SyntaxKind.NamespaceDeclaration:
                    return ((NamespaceDeclarationSyntax)node).Name.ToString();
                case SyntaxKind.OperatorDeclaration:
                    return "operator " + ((OperatorDeclarationSyntax)node).OperatorToken.ToString();
                case SyntaxKind.ConversionOperatorDeclaration:
                    var conversionOperator = (ConversionOperatorDeclarationSyntax)node;
                    return (conversionOperator.ImplicitOrExplicitKeyword.Kind() == SyntaxKind.ImplicitKeyword ? "implicit " : "explicit ")
                        + "operator "
                        + conversionOperator.Type.ToString();
                case SyntaxKind.EnumMemberDeclaration:
                    return ((EnumMemberDeclarationSyntax)node).Identifier.ToString();
                case SyntaxKind.VariableDeclarator:
                    return ((VariableDeclaratorSyntax)node).Identifier.ToString();
                case SyntaxKind.Attribute:
                    return ((AttributeSyntax)node).Name.ToString();
                case SyntaxKind.AttributeArgument:
                    var attributeArgument = (AttributeArgumentSyntax)node;
                    return attributeArgument.NameEquals != null
                        ? attributeArgument.NameEquals.Name.ToString()
                        : string.Empty;
                case SyntaxKind.UsingDirective:
                    throw Exceptions.ThrowEFail();
                default:
                    Debug.Fail("Invalid node kind: " + node.Kind());
                    throw new ArgumentException();
            }
        }

        public override SyntaxNode GetNodeWithName(SyntaxNode node)
        {
            var kind = node.Kind();
            if (kind == SyntaxKind.OperatorDeclaration ||
                kind == SyntaxKind.ConversionOperatorDeclaration)
            {
                throw Exceptions.ThrowEFail();
            }

            return node;
        }

        public override SyntaxNode SetName(SyntaxNode node, string name)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // In all cases, the resulting syntax for the new name has elastic trivia attached,
            // whether via this call to SyntaxFactory.Identifier or via explicitly added elastic
            // markers.
            var newIdentifier = SyntaxFactory.Identifier(name);
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                    return SetNameOfClassOrStruct(node, newIdentifier);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.EnumDeclaration:
                    return ((EnumDeclarationSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.DelegateDeclaration:
                    return ((DelegateDeclarationSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.EventDeclaration:
                    return ((EventDeclarationSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.Parameter:
                    return ((ParameterSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.NamespaceDeclaration:
                    return ((NamespaceDeclarationSyntax)node).WithName(
                        SyntaxFactory.ParseName(name)
                            .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker))
                            .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker)));
                case SyntaxKind.EnumMemberDeclaration:
                    return ((EnumMemberDeclarationSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.VariableDeclarator:
                    return ((VariableDeclaratorSyntax)node).WithIdentifier(newIdentifier);
                case SyntaxKind.Attribute:
                    return ((AttributeSyntax)node).WithName(
                        SyntaxFactory.ParseName(name)
                            .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker))
                            .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker)));
                case SyntaxKind.AttributeArgument:
                    return ((AttributeArgumentSyntax)node).WithNameEquals(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(name)));
                default:
                    Debug.Fail("Invalid node kind: " + node.Kind());
                    throw new ArgumentException();
            }
        }

        private static SyntaxNode SetNameOfClassOrStruct(SyntaxNode node, SyntaxToken newIdentifier)
        {
            var typeNode = (TypeDeclarationSyntax)node;

            foreach (var member in typeNode.Members)
            {
                if (member.Kind() == SyntaxKind.ConstructorDeclaration)
                {
                    var constructor = ((ConstructorDeclarationSyntax)member).WithIdentifier(newIdentifier);
                    typeNode = typeNode.ReplaceNode(member, constructor);
                }
                else if (member.Kind() == SyntaxKind.DestructorDeclaration)
                {
                    var destructor = ((DestructorDeclarationSyntax)member).WithIdentifier(newIdentifier);
                    typeNode = typeNode.ReplaceNode(member, destructor);
                }
            }

            if (typeNode.Kind() == SyntaxKind.StructDeclaration)
            {
                return ((StructDeclarationSyntax)typeNode).WithIdentifier(newIdentifier);
            }
            else
            {
                return ((ClassDeclarationSyntax)typeNode).WithIdentifier(newIdentifier);
            }
        }

        public override string GetFullName(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node.Kind() == SyntaxKind.UsingDirective)
            {
                throw Exceptions.ThrowEFail();
            }

            var symbol = node is AttributeSyntax
                ? semanticModel.GetTypeInfo(node).Type
                : semanticModel.GetDeclaredSymbol(node);

            return GetExternalSymbolFullName(symbol);
        }

        public override string GetFullyQualifiedName(string name, int position, SemanticModel semanticModel)
        {
            var typeName = SyntaxFactory.ParseTypeName(name);
            if (typeName is PredefinedTypeSyntax predefinedTypeNode)
            {
                if (SyntaxFactsService.TryGetPredefinedType(predefinedTypeNode.Keyword, out var predefinedType))
                {
                    var specialType = predefinedType.ToSpecialType();
                    return semanticModel.Compilation.GetSpecialType(specialType).GetEscapedFullName();
                }
            }
            else
            {
                var symbols = semanticModel.LookupNamespacesAndTypes(position, name: name);
                if (symbols.Length > 0)
                {
                    return symbols[0].GetEscapedFullName();
                }
            }

            return name;
        }

        public override bool IsValidExternalSymbol(ISymbol symbol)
        {
            if (symbol is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.MethodKind == MethodKind.PropertyGet ||
                    methodSymbol.MethodKind == MethodKind.PropertySet ||
                    methodSymbol.MethodKind == MethodKind.EventAdd ||
                    methodSymbol.MethodKind == MethodKind.EventRemove ||
                    methodSymbol.MethodKind == MethodKind.EventRaise)
                {
                    return false;
                }
            }

            return symbol.DeclaredAccessibility == Accessibility.Public
                || symbol.DeclaredAccessibility == Accessibility.Protected
                || symbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal;
        }

        public override string GetExternalSymbolName(ISymbol symbol)
        {
            if (symbol == null)
            {
                throw Exceptions.ThrowEFail();
            }

            return symbol.ToDisplayString(s_externalNameFormat);
        }

        public override string GetExternalSymbolFullName(ISymbol symbol)
        {
            if (symbol == null)
            {
                throw Exceptions.ThrowEFail();
            }

            return symbol.ToDisplayString(s_externalFullNameFormat);
        }

        public override EnvDTE.vsCMAccess GetAccess(ISymbol symbol)
            => symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => EnvDTE.vsCMAccess.vsCMAccessPublic,
                Accessibility.Private => EnvDTE.vsCMAccess.vsCMAccessPrivate,
                Accessibility.Internal => EnvDTE.vsCMAccess.vsCMAccessProject,
                Accessibility.Protected => EnvDTE.vsCMAccess.vsCMAccessProtected,
                Accessibility.ProtectedOrInternal => EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected,
                Accessibility.ProtectedAndInternal =>
                    // there is no appropriate mapping for private protected in EnvDTE.vsCMAccess
                    // See https://github.com/dotnet/roslyn/issues/22406
                    EnvDTE.vsCMAccess.vsCMAccessProtected,
                _ => throw Exceptions.ThrowEFail(),
            };

        public override EnvDTE.vsCMAccess GetAccess(SyntaxNode node)
        {
            var member = GetNodeWithModifiers(node);

            if (member == null)
            {
                throw Exceptions.ThrowEFail();
            }

            var modifiers = member.GetModifiers();

            if (modifiers.Any(t => t.Kind() == SyntaxKind.PublicKeyword))
            {
                return EnvDTE.vsCMAccess.vsCMAccessPublic;
            }
            else if (modifiers.Any(t => t.Kind() == SyntaxKind.ProtectedKeyword) && modifiers.Any(t => t.Kind() == SyntaxKind.InternalKeyword))
            {
                return EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected;
            }
            else if (modifiers.Any(t => t.Kind() == SyntaxKind.InternalKeyword))
            {
                return EnvDTE.vsCMAccess.vsCMAccessProject;
            }
            else if (modifiers.Any(t => t.Kind() == SyntaxKind.ProtectedKeyword))
            {
                return EnvDTE.vsCMAccess.vsCMAccessProtected;
            }
            else if (modifiers.Any(t => t.Kind() == SyntaxKind.PrivateKeyword))
            {
                return EnvDTE.vsCMAccess.vsCMAccessPrivate;
            }
            else
            {
                // The code does not specify the accessibility, so we need to
                // determine the default accessibility.
                return GetDefaultAccessibility(member);
            }
        }

        public override SyntaxNode GetNodeWithModifiers(SyntaxNode node)
        {
            return node is VariableDeclaratorSyntax
                   ? node.GetAncestor<MemberDeclarationSyntax>()
                   : node;
        }

        public override SyntaxNode GetNodeWithType(SyntaxNode node)
        {
            return node is VariableDeclaratorSyntax
                   ? node.GetAncestor<MemberDeclarationSyntax>()
                   : node;
        }

        public override SyntaxNode GetNodeWithInitializer(SyntaxNode node)
        {
            return node;
        }

        private EnvDTE.vsCMAccess GetDefaultAccessibility(SyntaxNode node)
        {
            if (node is EnumMemberDeclarationSyntax)
            {
                return EnvDTE.vsCMAccess.vsCMAccessPublic;
            }

            if (node is BaseFieldDeclarationSyntax ||
                node is BaseMethodDeclarationSyntax ||
                node is BasePropertyDeclarationSyntax)
            {
                // Members of interfaces and enums are public, while all other
                // members are private.
                if (node.HasAncestor<InterfaceDeclarationSyntax>() ||
                    node.HasAncestor<EnumDeclarationSyntax>())
                {
                    return EnvDTE.vsCMAccess.vsCMAccessPublic;
                }
                else
                {
                    return EnvDTE.vsCMAccess.vsCMAccessPrivate;
                }
            }

            if (node is BaseTypeDeclarationSyntax ||
                node is DelegateDeclarationSyntax)
            {
                // Types declared within types are private by default,
                // otherwise internal.
                if (node.HasAncestor<BaseTypeDeclarationSyntax>())
                {
                    return EnvDTE.vsCMAccess.vsCMAccessPrivate;
                }
                else
                {
                    return EnvDTE.vsCMAccess.vsCMAccessProject;
                }
            }

            if (node is AccessorDeclarationSyntax ||
                node is ArrowExpressionClauseSyntax)
            {
                return GetAccess(node.FirstAncestorOrSelf<BasePropertyDeclarationSyntax>());
            }

            throw Exceptions.ThrowEFail();
        }

        public override SyntaxNode SetAccess(SyntaxNode node, EnvDTE.vsCMAccess newAccess)
        {
            if (!(node is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            if (member.Parent.Kind() == SyntaxKind.InterfaceDeclaration ||
                member.Parent.Kind() == SyntaxKind.EnumDeclaration)
            {
                if (newAccess == EnvDTE.vsCMAccess.vsCMAccessDefault ||
                    newAccess == EnvDTE.vsCMAccess.vsCMAccessPublic)
                {
                    return member;
                }
                else
                {
                    throw Exceptions.ThrowEInvalidArg();
                }
            }

            if (member is BaseTypeDeclarationSyntax ||
                member is EnumDeclarationSyntax)
            {
                if (!(member.Parent is BaseTypeDeclarationSyntax) &&
                    (newAccess == EnvDTE.vsCMAccess.vsCMAccessPrivate ||
                     newAccess == EnvDTE.vsCMAccess.vsCMAccessProtected ||
                     newAccess == EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected))
                {
                    throw Exceptions.ThrowEInvalidArg();
                }
            }

            var modifierFlags = member.GetModifierFlags() & ~ModifierFlags.AccessModifierMask;

            switch (newAccess)
            {
                case EnvDTE.vsCMAccess.vsCMAccessPrivate:
                    modifierFlags |= ModifierFlags.Private;
                    break;

                case EnvDTE.vsCMAccess.vsCMAccessProtected:
                    modifierFlags |= ModifierFlags.Protected;
                    break;

                case EnvDTE.vsCMAccess.vsCMAccessPublic:
                    modifierFlags |= ModifierFlags.Public;
                    break;

                case EnvDTE.vsCMAccess.vsCMAccessProject:
                    modifierFlags |= ModifierFlags.Internal;
                    break;

                case EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected:
                    modifierFlags |= ModifierFlags.Protected | ModifierFlags.Internal;
                    break;

                case EnvDTE.vsCMAccess.vsCMAccessDefault:
                    break; // No change

                default:
                    throw Exceptions.ThrowEUnexpected();
            }

            return member.UpdateModifiers(modifierFlags);
        }

        private IList<SyntaxTrivia> CollectComments(IList<SyntaxTrivia> triviaList)
        {
            var commentList = new List<SyntaxTrivia>();

            for (var i = triviaList.Count - 1; i >= 0; i--)
            {
                var trivia = triviaList[i];
                if (trivia.IsRegularComment())
                {
                    commentList.Add(trivia);
                }
                else if (trivia.Kind() != SyntaxKind.WhitespaceTrivia &&
                    trivia.Kind() != SyntaxKind.EndOfLineTrivia)
                {
                    break;
                }
            }

            commentList.Reverse();

            return commentList;
        }

        public override string GetComment(SyntaxNode node)
        {
            var firstToken = node.GetFirstToken();
            var commentList = CollectComments(firstToken.LeadingTrivia.ToArray());

            if (commentList.Count == 0)
            {
                return string.Empty;
            }

            var textBuilder = new StringBuilder();
            foreach (var trivia in commentList)
            {
                if (trivia.IsRegularComment())
                {
                    textBuilder.AppendLine(trivia.GetCommentText());
                }
                else
                {
                    throw Exceptions.ThrowEFail();
                }
            }

            return textBuilder.ToString();
        }

        public override SyntaxNode SetComment(SyntaxNode node, string value)
        {
            Debug.Assert(node is MemberDeclarationSyntax);

            var memberDeclaration = (MemberDeclarationSyntax)node;
            var text = memberDeclaration.SyntaxTree.GetText(CancellationToken.None);
            var newLine = GetNewLineCharacter(text);

            var commentText = string.Empty;

            if (value != null)
            {
                var builder = new StringBuilder();

                foreach (var line in value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    builder.Append("// ");
                    builder.Append(line);
                    builder.Append(newLine);
                }

                commentText = builder.ToString();
            }

            var newTriviaList = SyntaxFactory.ParseLeadingTrivia(commentText);
            var leadingTriviaList = memberDeclaration.GetLeadingTrivia().ToList();

            var commentList = CollectComments(leadingTriviaList);
            if (commentList.Count > 0)
            {
                // In this case, we're going to replace the existing comment.
                var firstIndex = leadingTriviaList.FindIndex(t => t == commentList[0]);
                var lastIndex = leadingTriviaList.FindIndex(t => t == commentList[commentList.Count - 1]);
                var count = lastIndex - firstIndex + 1;

                leadingTriviaList.RemoveRange(firstIndex, count);

                // Note: single line comments have a trailing new-line but that won't be
                // returned by CollectComments. So, we may need to remove an additional new line below.
                if (firstIndex < leadingTriviaList.Count &&
                    leadingTriviaList[firstIndex].Kind() == SyntaxKind.EndOfLineTrivia)
                {
                    leadingTriviaList.RemoveAt(firstIndex);
                }

                foreach (var trivia in newTriviaList.Reverse())
                {
                    leadingTriviaList.Insert(firstIndex, trivia);
                }
            }
            else
            {
                // Otherwise, just add the comment to the end of the leading trivia.
                leadingTriviaList.AddRange(newTriviaList);
            }

            return memberDeclaration.WithLeadingTrivia(leadingTriviaList);
        }

        private static DocumentationCommentTriviaSyntax GetDocCommentNode(MemberDeclarationSyntax memberDeclaration)
        {
            var docCommentTrivia = memberDeclaration
                .GetLeadingTrivia()
                .Reverse()
                .FirstOrDefault(t => t.IsDocComment());

            if (!docCommentTrivia.IsDocComment())
            {
                return null;
            }

            return (DocumentationCommentTriviaSyntax)docCommentTrivia.GetStructure();
        }

        public override string GetDocComment(SyntaxNode node)
        {
            Debug.Assert(node is MemberDeclarationSyntax);

            var memberDeclaration = (MemberDeclarationSyntax)node;
            var documentationComment = GetDocCommentNode(memberDeclaration);
            if (documentationComment == null)
            {
                return string.Empty;
            }

            var text = memberDeclaration.SyntaxTree.GetText(CancellationToken.None);
            var newLine = GetNewLineCharacter(text);

            var lines = documentationComment.ToString().Split(new[] { newLine }, StringSplitOptions.None);

            // trim off leading whitespace and exterior trivia.
            var lengthToStrip = lines[0].GetLeadingWhitespace().Length;
            var linesCount = lines.Length;

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].TrimStart();
                if (line.StartsWith("///", StringComparison.Ordinal))
                {
                    line = line.Substring(3);
                }

                if (line.Length > 0)
                {
                    lengthToStrip = Math.Min(lengthToStrip, line.GetLeadingWhitespace().Length);
                }

                lines[i] = line;
            }

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length > lengthToStrip)
                {
                    lines[i] = line.Substring(lengthToStrip);
                }
            }

            return "<doc>\r\n" + lines.Join(newLine) + "</doc>";
        }

        public override SyntaxNode SetDocComment(SyntaxNode node, string value)
        {
            Debug.Assert(node is MemberDeclarationSyntax);

            XDocument xmlDocument;
            try
            {
                using var reader = new StringReader(value);
                xmlDocument = XDocument.Load(reader);
            }
            catch
            {
                xmlDocument = null;
            }

            if (xmlDocument == null)
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            if (!(xmlDocument.FirstNode is XElement docElement) ||
                docElement.Name.ToString().ToLower() != "doc")
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            var memberDeclaration = (MemberDeclarationSyntax)node;
            var text = memberDeclaration.SyntaxTree.GetText(CancellationToken.None);
            var newLine = GetNewLineCharacter(text);
            var builder = new StringBuilder();

            foreach (var child in docElement.Elements())
            {
                foreach (var line in child.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    builder.Append("/// ");
                    builder.Append(line);
                    builder.Append(newLine);
                }
            }

            var newTriviaList = SyntaxFactory.ParseLeadingTrivia(builder.ToString());
            var leadingTriviaList = memberDeclaration.GetLeadingTrivia().ToList();
            var documentationComment = GetDocCommentNode(memberDeclaration);

            if (documentationComment != null)
            {
                // In this case, we're going to replace the existing XML doc comment.
                var index = leadingTriviaList.FindIndex(t => t == documentationComment.ParentTrivia);
                leadingTriviaList.RemoveAt(index);

                foreach (var triviaElement in newTriviaList.Reverse())
                {
                    leadingTriviaList.Insert(index, triviaElement);
                }
            }
            else
            {
                // Otherwise, just add the XML doc comment to the end of the leading trivia.
                leadingTriviaList.AddRange(newTriviaList);
            }

            return memberDeclaration.WithLeadingTrivia(leadingTriviaList);
        }

        public override IEnumerable<SyntaxNode> GetParameterNodes(SyntaxNode parentNode)
        {
            if (parentNode is BaseMethodDeclarationSyntax baseMethod)
            {
                return baseMethod.ParameterList.Parameters;
            }
            else if (parentNode is IndexerDeclarationSyntax indexer)
            {
                return indexer.ParameterList.Parameters;
            }
            else if (parentNode is DelegateDeclarationSyntax delegateDecl)
            {
                return delegateDecl.ParameterList.Parameters;
            }

            return SpecializedCollections.EmptyEnumerable<ParameterSyntax>();
        }

        public override bool IsExpressionBodiedProperty(SyntaxNode node)
            => (node as PropertyDeclarationSyntax)?.ExpressionBody != null;

        public override bool TryGetAutoPropertyExpressionBody(SyntaxNode parentNode, out SyntaxNode accessorNode)
        {
            accessorNode = (parentNode as PropertyDeclarationSyntax)?.ExpressionBody;
            return accessorNode != null;
        }

        public override bool IsAccessorNode(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return true;
            }

            return false;
        }

        public override MethodKind GetAccessorKind(SyntaxNode node)
            => node.Kind() switch
            {
                SyntaxKind.GetAccessorDeclaration => MethodKind.PropertyGet,
                SyntaxKind.SetAccessorDeclaration => MethodKind.PropertySet,
                SyntaxKind.AddAccessorDeclaration => MethodKind.EventAdd,
                SyntaxKind.RemoveAccessorDeclaration => MethodKind.EventRemove,
                _ => throw Exceptions.ThrowEUnexpected(),
            };

        private static SyntaxKind GetAccessorSyntaxKind(MethodKind methodKind)
            => methodKind switch
            {
                MethodKind.PropertyGet => SyntaxKind.GetAccessorDeclaration,
                MethodKind.PropertySet => SyntaxKind.SetAccessorDeclaration,
                MethodKind.EventAdd => SyntaxKind.AddAccessorDeclaration,
                MethodKind.EventRemove => SyntaxKind.RemoveAccessorDeclaration,
                _ => throw Exceptions.ThrowEUnexpected(),
            };

        public override bool TryGetAccessorNode(SyntaxNode parentNode, MethodKind kind, out SyntaxNode accessorNode)
        {
            Debug.Assert(parentNode is BasePropertyDeclarationSyntax);

            var basePropertyDeclaration = (BasePropertyDeclarationSyntax)parentNode;
            var accessorKind = GetAccessorSyntaxKind(kind);

            if (basePropertyDeclaration.AccessorList != null)
            {
                foreach (var accessor in basePropertyDeclaration.AccessorList.Accessors)
                {
                    if (accessor.Kind() == accessorKind)
                    {
                        accessorNode = accessor;
                        return true;
                    }
                }
            }

            accessorNode = null;
            return false;
        }

        public override bool TryGetParameterNode(SyntaxNode parentNode, string name, out SyntaxNode parameterNode)
        {
            foreach (ParameterSyntax parameter in GetParameterNodes(parentNode))
            {
                if (parameter.Identifier.ToString() == name)
                {
                    parameterNode = parameter;
                    return true;
                }
            }

            parameterNode = null;
            return false;
        }

        public override bool TryGetImportNode(SyntaxNode parentNode, string dottedName, out SyntaxNode importNode)
        {
            foreach (UsingDirectiveSyntax usingDirective in GetImportNodes(parentNode))
            {
                if (usingDirective.Name.ToString() == dottedName)
                {
                    importNode = usingDirective;
                    return true;
                }
            }

            importNode = null;
            return false;
        }

        public override bool TryGetOptionNode(SyntaxNode parentNode, string name, int ordinal, out SyntaxNode optionNode)
        {
            // Only VB has Option statements
            throw new NotSupportedException();
        }

        public override bool TryGetInheritsNode(SyntaxNode parentNode, string name, int ordinal, out SyntaxNode inheritsNode)
        {
            // Only VB has Inherits statements
            throw new NotSupportedException();
        }

        public override bool TryGetImplementsNode(SyntaxNode parentNode, string name, int ordinal, out SyntaxNode implementsNode)
        {
            // Only VB has Implements statements
            throw new NotSupportedException();
        }

        public override bool TryGetAttributeNode(SyntaxNode parentNode, string name, int ordinal, out SyntaxNode attributeNode)
        {
            var count = -1;
            foreach (AttributeSyntax attribute in GetAttributeNodes(parentNode))
            {
                if (attribute.Name.ToString() == name)
                {
                    count++;
                    if (count == ordinal)
                    {
                        attributeNode = attribute;
                        return true;
                    }
                }
            }

            attributeNode = null;
            return false;
        }

        public override bool TryGetAttributeArgumentNode(SyntaxNode attributeNode, int index, out SyntaxNode attributeArgumentNode)
        {
            Debug.Assert(attributeNode is AttributeSyntax);

            var attribute = (AttributeSyntax)attributeNode;
            if (attribute.ArgumentList != null &&
                attribute.ArgumentList.Arguments.Count > index)
            {
                attributeArgumentNode = attribute.ArgumentList.Arguments[index];
                return true;
            }

            attributeArgumentNode = null;
            return false;
        }

        public override void GetOptionNameAndOrdinal(SyntaxNode parentNode, SyntaxNode optionNode, out string name, out int ordinal)
        {
            // Only VB supports Option statements
            throw new NotSupportedException();
        }

        public override void GetInheritsNamespaceAndOrdinal(SyntaxNode parentNode, SyntaxNode inheritsNode, out string namespaceName, out int ordinal)
        {
            // Only VB supports Inherits statements
            throw new NotSupportedException();
        }

        public override void GetImplementsNamespaceAndOrdinal(SyntaxNode parentNode, SyntaxNode implementsNode, out string namespaceName, out int ordinal)
        {
            // Only VB supports Implements statements
            throw new NotSupportedException();
        }

        public override void GetAttributeNameAndOrdinal(SyntaxNode parentNode, SyntaxNode attributeNode, out string name, out int ordinal)
        {
            Debug.Assert(attributeNode is AttributeSyntax);

            name = ((AttributeSyntax)attributeNode).Name.ToString();

            ordinal = -1;
            foreach (AttributeSyntax attribute in GetAttributeNodes(parentNode))
            {
                if (attribute.Name.ToString() == name)
                {
                    ordinal++;
                }

                if (attribute == attributeNode)
                {
                    break;
                }
            }
        }

        public override void GetAttributeArgumentParentAndIndex(SyntaxNode attributeArgumentNode, out SyntaxNode attributeNode, out int index)
        {
            Debug.Assert(attributeArgumentNode is AttributeArgumentSyntax);

            var argument = (AttributeArgumentSyntax)attributeArgumentNode;
            var attribute = (AttributeSyntax)argument.Ancestors().First(n => n.Kind() == SyntaxKind.Attribute);

            attributeNode = attribute;
            index = attribute.ArgumentList.Arguments.IndexOf((AttributeArgumentSyntax)attributeArgumentNode);
        }

        public override SyntaxNode GetAttributeTargetNode(SyntaxNode attributeNode)
        {
            Debug.Assert(attributeNode is AttributeSyntax);
            Debug.Assert(attributeNode.Parent is AttributeListSyntax);

            return (AttributeListSyntax)attributeNode.Parent;
        }

        public override string GetAttributeTarget(SyntaxNode attributeNode)
        {
            Debug.Assert(attributeNode is AttributeSyntax);
            Debug.Assert(attributeNode.Parent is AttributeListSyntax);

            var attributeList = (AttributeListSyntax)attributeNode.Parent;
            if (attributeList.Target != null)
            {
                return attributeList.Target.Identifier.ToString();
            }

            return string.Empty;
        }

        public override SyntaxNode SetAttributeTarget(SyntaxNode attributeNode, string target)
        {
            Debug.Assert(attributeNode is AttributeListSyntax);

            var attributeList = (AttributeListSyntax)attributeNode;

            if (string.IsNullOrEmpty(target))
            {
                return attributeList.WithTarget(null);
            }
            else
            {
                return attributeList.WithTarget(
                    SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Identifier(target)));
            }
        }

        public override string GetAttributeValue(SyntaxNode attributeNode)
        {
            Debug.Assert(attributeNode is AttributeSyntax);

            var attribute = (AttributeSyntax)attributeNode;
            var argumentList = attribute.ArgumentList;
            if (argumentList != null)
            {
                return argumentList.Arguments.ToString();
            }

            return string.Empty;
        }

        public override SyntaxNode SetAttributeValue(SyntaxNode attributeNode, string value)
        {
            Debug.Assert(attributeNode is AttributeSyntax);

            var attribute = (AttributeSyntax)attributeNode;
            var argumentList = attribute.ArgumentList;
            var parsedArgumentList = SyntaxFactory.ParseAttributeArgumentList("(" + value + ")");
            var newArgumentList = argumentList != null
                ? argumentList.WithArguments(parsedArgumentList.Arguments)
                : parsedArgumentList;

            return attribute.WithArgumentList(newArgumentList);
        }

        public override SyntaxNode GetNodeWithAttributes(SyntaxNode node)
        {
            return node is VariableDeclaratorSyntax
                   ? node.GetAncestor<MemberDeclarationSyntax>()
                   : node;
        }

        public override SyntaxNode GetEffectiveParentForAttribute(SyntaxNode node)
        {
            if (node.HasAncestor<BaseFieldDeclarationSyntax>())
            {
                return node.GetAncestor<BaseFieldDeclarationSyntax>().Declaration.Variables.FirstOrDefault();
            }
            else if (node.HasAncestor<ParameterSyntax>())
            {
                return node.GetAncestor<ParameterSyntax>();
            }
            else
            {
                return node.Parent;
            }
        }

        public override SyntaxNode CreateAttributeNode(string name, string value, string target = null)
        {
            var specifier = target != null
                ? SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Identifier(target),
                    SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker), SyntaxKind.ColonToken, SyntaxFactory.TriviaList(SyntaxFactory.Space)))
                : null;

            return SyntaxFactory.AttributeList(
                target: specifier,
                attributes: SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                        name: SyntaxFactory.ParseName(name),
                        argumentList: SyntaxFactory.ParseAttributeArgumentList("(" + value + ")"))));
        }

        public override SyntaxNode CreateAttributeArgumentNode(string name, string value)
        {
            if (!string.IsNullOrEmpty(name))
            {
                return SyntaxFactory.AttributeArgument(
                    nameEquals: SyntaxFactory.NameEquals(name),
                    nameColon: null,
                    expression: SyntaxFactory.ParseExpression(value));
            }
            else
            {
                return SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(value));
            }
        }

        public override SyntaxNode CreateImportNode(string name, string alias = null)
        {
            var nameSyntax = SyntaxFactory.ParseName(name);

            if (!string.IsNullOrEmpty(alias))
            {
                var aliasSyntax = SyntaxFactory.NameEquals(alias);
                return SyntaxFactory.UsingDirective(aliasSyntax, nameSyntax);
            }
            else
            {
                return SyntaxFactory.UsingDirective(nameSyntax);
            }
        }

        public override SyntaxNode CreateParameterNode(string name, string type)
        {
            return SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)).WithType(SyntaxFactory.ParseTypeName(type));
        }

        public override string GetAttributeArgumentValue(SyntaxNode attributeArgumentNode)
        {
            Debug.Assert(attributeArgumentNode is AttributeArgumentSyntax);

            return ((AttributeArgumentSyntax)attributeArgumentNode).Expression.ToString();
        }

        public override string GetImportAlias(SyntaxNode importNode)
        {
            if (importNode is UsingDirectiveSyntax usingDirective)
            {
                return usingDirective.Alias != null
                    ? usingDirective.Alias.Name.ToString()
                    : string.Empty;
            }

            throw new InvalidOperationException();
        }

        public override string GetImportNamespaceOrType(SyntaxNode importNode)
        {
            if (importNode is UsingDirectiveSyntax usingDirective)
            {
                return usingDirective.Name.ToString();
            }

            throw new InvalidOperationException();
        }

        public override void GetImportParentAndName(SyntaxNode importNode, out SyntaxNode namespaceNode, out string name)
        {
            if (importNode is UsingDirectiveSyntax usingDirective)
            {
                namespaceNode = usingDirective.Parent.Kind() == SyntaxKind.CompilationUnit
                    ? null
                    : usingDirective.Parent;

                name = usingDirective.Name.ToString();

                return;
            }

            throw new InvalidOperationException();
        }

        public override string GetParameterName(SyntaxNode node)
        {
            if (node is ParameterSyntax parameter)
            {
                return parameter.Identifier.ToString();
            }

            throw new InvalidOperationException();
        }

        public override EnvDTE80.vsCMParameterKind GetParameterKind(SyntaxNode node)
        {
            if (node is ParameterSyntax parameter)
            {
                var kind = EnvDTE80.vsCMParameterKind.vsCMParameterKindNone;

                var modifiers = parameter.Modifiers;
                if (modifiers.Any(SyntaxKind.RefKeyword))
                {
                    kind = EnvDTE80.vsCMParameterKind.vsCMParameterKindRef;
                }
                else if (modifiers.Any(SyntaxKind.OutKeyword))
                {
                    kind = EnvDTE80.vsCMParameterKind.vsCMParameterKindOut;
                }
                else if (modifiers.Any(SyntaxKind.ParamsKeyword))
                {
                    kind = EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray;
                }

                // Note: this is not an "else if" since it might be both
                // optional and "ref".
                if (parameter.Default != null)
                {
                    kind |= EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional;
                }

                return kind;
            }

            throw new InvalidOperationException();
        }

        public override SyntaxNode SetParameterKind(SyntaxNode node, EnvDTE80.vsCMParameterKind kind)
        {
            if (!(node is ParameterSyntax parameter))
            {
                throw Exceptions.ThrowEFail();
            }

            // We can't do anything with "Optional", so just strip it out and ignore it.
            if ((kind & EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional) != 0)
            {
                kind -= EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional;
            }

            SyntaxTokenList newModifiers;

            switch (kind)
            {
                case EnvDTE80.vsCMParameterKind.vsCMParameterKindOut:
                    newModifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OutKeyword));
                    break;

                case EnvDTE80.vsCMParameterKind.vsCMParameterKindRef:
                    newModifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword));
                    break;

                case EnvDTE80.vsCMParameterKind.vsCMParameterKindIn:
                case EnvDTE80.vsCMParameterKind.vsCMParameterKindNone:
                    newModifiers = SyntaxFactory.TokenList();
                    break;

                case EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray:
                    {
                        var parameterList = (ParameterListSyntax)parameter.Parent;
                        if (parameterList.Parameters.LastOrDefault() == parameter &&
                            parameter.Type is ArrayTypeSyntax)
                        {
                            newModifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ParamsKeyword));
                            break;
                        }

                        throw Exceptions.ThrowEInvalidArg();
                    }

                default:
                    throw Exceptions.ThrowEInvalidArg();
            }

            return parameter.WithModifiers(newModifiers);
        }

        public override EnvDTE80.vsCMParameterKind UpdateParameterKind(EnvDTE80.vsCMParameterKind parameterKind, PARAMETER_PASSING_MODE passingMode)
        {
            var updatedParameterKind = parameterKind;

            switch (passingMode)
            {
                case PARAMETER_PASSING_MODE.cmParameterTypeIn:
                    updatedParameterKind |= EnvDTE80.vsCMParameterKind.vsCMParameterKindNone;
                    updatedParameterKind &= ~EnvDTE80.vsCMParameterKind.vsCMParameterKindRef;
                    updatedParameterKind &= ~EnvDTE80.vsCMParameterKind.vsCMParameterKindOut;
                    break;

                case PARAMETER_PASSING_MODE.cmParameterTypeInOut:
                    updatedParameterKind &= ~EnvDTE80.vsCMParameterKind.vsCMParameterKindNone;
                    updatedParameterKind |= EnvDTE80.vsCMParameterKind.vsCMParameterKindRef;
                    updatedParameterKind &= ~EnvDTE80.vsCMParameterKind.vsCMParameterKindOut;
                    break;

                case PARAMETER_PASSING_MODE.cmParameterTypeOut:
                    updatedParameterKind &= ~EnvDTE80.vsCMParameterKind.vsCMParameterKindNone;
                    updatedParameterKind &= ~EnvDTE80.vsCMParameterKind.vsCMParameterKindRef;
                    updatedParameterKind |= EnvDTE80.vsCMParameterKind.vsCMParameterKindOut;
                    break;
            }

            return updatedParameterKind;
        }

        public override EnvDTE.vsCMFunction ValidateFunctionKind(SyntaxNode containerNode, EnvDTE.vsCMFunction kind, string name)
        {
            if (kind == EnvDTE.vsCMFunction.vsCMFunctionSub)
            {
                return EnvDTE.vsCMFunction.vsCMFunctionFunction;
            }

            if (kind == EnvDTE.vsCMFunction.vsCMFunctionFunction)
            {
                return kind;
            }

            if (kind == EnvDTE.vsCMFunction.vsCMFunctionConstructor ||
                kind == EnvDTE.vsCMFunction.vsCMFunctionDestructor)
            {
                if (containerNode is InterfaceDeclarationSyntax)
                {
                    throw Exceptions.ThrowEFail();
                }

                return kind;
            }

            throw Exceptions.ThrowENotImpl();
        }

        public override bool SupportsEventThrower
        {
            get { return false; }
        }

        public override bool GetCanOverride(SyntaxNode memberNode)
        {
            Debug.Assert(memberNode is MemberDeclarationSyntax);

            if (!(memberNode is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            if (member.Parent is InterfaceDeclarationSyntax)
            {
                return true;
            }

            var flags = member.GetModifierFlags();

            return (flags & (ModifierFlags.Abstract | ModifierFlags.Virtual)) != 0;
        }

        public override SyntaxNode SetCanOverride(SyntaxNode memberNode, bool value)
        {
            Debug.Assert(memberNode is MemberDeclarationSyntax);

            if (!(memberNode is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            if (member.Parent is InterfaceDeclarationSyntax)
            {
                if (!value)
                {
                    throw Exceptions.ThrowEInvalidArg();
                }

                return memberNode;
            }

            var flags = member.GetModifierFlags();

            if (value)
            {
                flags |= ModifierFlags.Virtual;
            }
            else
            {
                flags &= ~ModifierFlags.Virtual;
            }

            return member.UpdateModifiers(flags);
        }

        public override EnvDTE80.vsCMClassKind GetClassKind(SyntaxNode typeNode, INamedTypeSymbol typeSymbol)
        {
            Debug.Assert(typeNode is ClassDeclarationSyntax);

            var type = (ClassDeclarationSyntax)typeNode;
            var flags = type.GetModifierFlags();

            return (flags & ModifierFlags.Partial) != 0
                ? EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass
                : EnvDTE80.vsCMClassKind.vsCMClassKindMainClass;
        }

        public override SyntaxNode SetClassKind(SyntaxNode typeNode, EnvDTE80.vsCMClassKind kind)
        {
            Debug.Assert(typeNode is ClassDeclarationSyntax);

            var type = (ClassDeclarationSyntax)typeNode;
            var flags = type.GetModifierFlags();

            if (kind == EnvDTE80.vsCMClassKind.vsCMClassKindPartialClass)
            {
                flags |= ModifierFlags.Partial;
            }
            else if (kind == EnvDTE80.vsCMClassKind.vsCMClassKindMainClass)
            {
                flags &= ~ModifierFlags.Partial;
            }

            return type.UpdateModifiers(flags);
        }

        public override EnvDTE80.vsCMConstKind GetConstKind(SyntaxNode variableNode)
        {
            if (variableNode is EnumMemberDeclarationSyntax)
            {
                return EnvDTE80.vsCMConstKind.vsCMConstKindConst;
            }

            if (!(GetNodeWithModifiers(variableNode) is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            var flags = member.GetModifierFlags();

            var result = EnvDTE80.vsCMConstKind.vsCMConstKindNone;
            if ((flags & ModifierFlags.ReadOnly) != 0)
            {
                result |= EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly;
            }

            if ((flags & ModifierFlags.Const) != 0)
            {
                result |= EnvDTE80.vsCMConstKind.vsCMConstKindConst;
            }

            // Note: It's possible that we could return vsCMConstKindCont | vsCMConstKindReadOnly if the
            // user had incorrectly specified both const and readonly in their code. That's OK since
            // Code Model represents the source.

            return result;
        }

        public override SyntaxNode SetConstKind(SyntaxNode variableNode, EnvDTE80.vsCMConstKind kind)
        {
            Debug.Assert(variableNode is FieldDeclarationSyntax ||
                         variableNode is EnumMemberDeclarationSyntax);

            if (variableNode is EnumMemberDeclarationSyntax)
            {
                if (kind != EnvDTE80.vsCMConstKind.vsCMConstKindConst &&
                    kind != EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
                {
                    throw Exceptions.ThrowEInvalidArg();
                }

                return variableNode;
            }

            var member = (MemberDeclarationSyntax)variableNode;
            var flags = member.GetModifierFlags();
            flags &= ~(ModifierFlags.Const | ModifierFlags.ReadOnly);

            switch (kind)
            {
                case EnvDTE80.vsCMConstKind.vsCMConstKindConst:
                    flags |= ModifierFlags.Const;
                    break;
                case EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly:
                    flags |= ModifierFlags.ReadOnly;
                    break;
            }

            return member.UpdateModifiers(flags);
        }

        public override EnvDTE80.vsCMDataTypeKind GetDataTypeKind(SyntaxNode typeNode, INamedTypeSymbol symbol)
        {
            Debug.Assert(typeNode is BaseTypeDeclarationSyntax);

            var type = (BaseTypeDeclarationSyntax)typeNode;
            var flags = type.GetModifierFlags();

            return (flags & ModifierFlags.Partial) != 0
                ? EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial
                : EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain;
        }

        public override SyntaxNode SetDataTypeKind(SyntaxNode typeNode, EnvDTE80.vsCMDataTypeKind kind)
        {
            Debug.Assert(typeNode is BaseTypeDeclarationSyntax);

            var type = (BaseTypeDeclarationSyntax)typeNode;
            var flags = type.GetModifierFlags();

            if (kind == EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial)
            {
                flags |= ModifierFlags.Partial;
            }
            else if (kind == EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain)
            {
                flags &= ~ModifierFlags.Partial;
            }

            return type.UpdateModifiers(flags);
        }

        public override EnvDTE.vsCMFunction GetFunctionKind(IMethodSymbol symbol)
        {
            switch (symbol.MethodKind)
            {
                case MethodKind.Ordinary:
                case MethodKind.ExplicitInterfaceImplementation:
                    return EnvDTE.vsCMFunction.vsCMFunctionFunction;

                case MethodKind.Constructor:
                case MethodKind.StaticConstructor:
                    return EnvDTE.vsCMFunction.vsCMFunctionConstructor;

                case MethodKind.Destructor:
                    return EnvDTE.vsCMFunction.vsCMFunctionDestructor;

                case MethodKind.UserDefinedOperator:
                case MethodKind.Conversion:
                    return EnvDTE.vsCMFunction.vsCMFunctionOperator;

                case MethodKind.PropertyGet:
                case MethodKind.EventRemove:
                    return EnvDTE.vsCMFunction.vsCMFunctionPropertyGet;

                case MethodKind.PropertySet:
                case MethodKind.EventAdd:
                    return EnvDTE.vsCMFunction.vsCMFunctionPropertySet;

                default:
                    throw Exceptions.ThrowEUnexpected();
            }
        }

        public override EnvDTE80.vsCMInheritanceKind GetInheritanceKind(SyntaxNode typeNode, INamedTypeSymbol typeSymbol)
        {
            Debug.Assert(typeNode is ClassDeclarationSyntax);

            var type = (ClassDeclarationSyntax)typeNode;
            var flags = type.GetModifierFlags();

            var result = EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone;

            if ((flags & ModifierFlags.Abstract) != 0)
            {
                result |= EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract;
            }

            if ((flags & ModifierFlags.New) != 0)
            {
                result |= EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew;
            }

            if ((flags & ModifierFlags.Sealed) != 0)
            {
                result |= EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed;
            }

            return result;
        }

        public override bool GetMustImplement(SyntaxNode memberNode)
        {
            Debug.Assert(memberNode is MemberDeclarationSyntax);

            if (!(memberNode is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            if (member.Parent is InterfaceDeclarationSyntax)
            {
                return true;
            }

            var flags = member.GetModifierFlags();

            return (flags & ModifierFlags.Abstract) != 0;
        }

        public override SyntaxNode SetMustImplement(SyntaxNode memberNode, bool value)
        {
            Debug.Assert(memberNode is MethodDeclarationSyntax ||
                         memberNode is BasePropertyDeclarationSyntax ||
                         memberNode is EventFieldDeclarationSyntax);

            if (!(memberNode is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            if (member.Parent is InterfaceDeclarationSyntax)
            {
                if (!value)
                {
                    throw Exceptions.ThrowEInvalidArg();
                }

                return memberNode;
            }

            // If this is a class member and the class is not abstract, we throw.
            if (member.Parent is ClassDeclarationSyntax)
            {
                var parentFlags = ((ClassDeclarationSyntax)member.Parent).GetModifierFlags();

                if (value && (parentFlags & ModifierFlags.Abstract) == 0)
                {
                    throw Exceptions.ThrowEInvalidArg();
                }
            }

            var flags = member.GetModifierFlags();

            if (value)
            {
                flags |= ModifierFlags.Abstract;

                // If this is a method, remove the body if it is empty.
                if (member is MethodDeclarationSyntax method)
                {
                    if (method.Body != null && method.Body.Statements.Count == 0)
                    {
                        member = method.WithBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker), SyntaxKind.SemicolonToken, method.Body.CloseBraceToken.TrailingTrivia));
                    }
                }
                else
                {
                    // If this is a property, remove the bodies of the accessors if they are empty.
                    // Note that "empty" means that the bodies contain no statements or just a single return statement.
                    if (member is BasePropertyDeclarationSyntax property && property.AccessorList != null)
                    {
                        var updatedAccessors = new List<AccessorDeclarationSyntax>();
                        foreach (var accessor in property.AccessorList.Accessors)
                        {
                            if (accessor.Body == null ||
                                accessor.Body.Statements.Count > 1 ||
                                (accessor.Body.Statements.Count == 1 && !accessor.Body.Statements[0].IsKind(SyntaxKind.ReturnStatement)))
                            {
                                // Leave this accessor as is
                                updatedAccessors.Add(accessor);
                                continue;
                            }

                            var updatedAccessor = accessor.WithBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker), SyntaxKind.SemicolonToken, accessor.Body.CloseBraceToken.TrailingTrivia));
                            updatedAccessors.Add(updatedAccessor);
                        }

                        var updatedAccessorList = property.AccessorList.WithAccessors(SyntaxFactory.List<AccessorDeclarationSyntax>(updatedAccessors));
                        member = property.ReplaceNode(property.AccessorList, updatedAccessorList);
                    }
                }
            }
            else
            {
                flags &= ~ModifierFlags.Abstract;

                // If this is a method, add a body.
                if (member is MethodDeclarationSyntax method)
                {
                    if (method.Body == null)
                    {
                        var newBody = SyntaxFactory.Block();
                        newBody = newBody.WithCloseBraceToken(newBody.CloseBraceToken.WithTrailingTrivia(method.SemicolonToken.TrailingTrivia));
                        member = method.WithSemicolonToken(default).WithBody(newBody);
                    }
                }
                else
                {
                    // If this is a property, add bodies to the accessors if they don't have them.
                    if (member is BasePropertyDeclarationSyntax property && property.AccessorList != null)
                    {
                        var updatedAccessors = new List<AccessorDeclarationSyntax>();
                        foreach (var accessor in property.AccessorList.Accessors)
                        {
                            if (accessor.Body != null)
                            {
                                // Leave this accessor as is
                                updatedAccessors.Add(accessor);
                                continue;
                            }

                            var newBody = SyntaxFactory.Block();
                            newBody = newBody.WithCloseBraceToken(newBody.CloseBraceToken.WithTrailingTrivia(accessor.SemicolonToken.TrailingTrivia));
                            var updatedAccessor = accessor.WithSemicolonToken(default).WithBody(newBody);
                            updatedAccessors.Add(updatedAccessor);
                        }

                        var updatedAccessorList = property.AccessorList.WithAccessors(SyntaxFactory.List<AccessorDeclarationSyntax>(updatedAccessors));
                        member = property.ReplaceNode(property.AccessorList, updatedAccessorList);
                    }
                }
            }

            return member.UpdateModifiers(flags);
        }

        public override SyntaxNode SetInheritanceKind(SyntaxNode typeNode, EnvDTE80.vsCMInheritanceKind kind)
        {
            Debug.Assert(typeNode is ClassDeclarationSyntax);

            if (!(typeNode is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            var flags = member.GetModifierFlags();
            flags &= ~(ModifierFlags.Abstract | ModifierFlags.New | ModifierFlags.Sealed);

            if (kind != EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone)
            {
                if ((kind & EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract) != 0)
                {
                    flags |= ModifierFlags.Abstract;
                }
                else if ((kind & EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed) != 0)
                {
                    flags |= ModifierFlags.Sealed;
                }

                // Can have new in combination with the above flags
                if ((kind & EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNew) != 0)
                {
                    flags |= ModifierFlags.New;
                }
            }

            return member.UpdateModifiers(flags);
        }

        public override EnvDTE80.vsCMOverrideKind GetOverrideKind(SyntaxNode memberNode)
        {
            Debug.Assert(memberNode is BaseMethodDeclarationSyntax ||
                         memberNode is BasePropertyDeclarationSyntax ||
                         memberNode is EventFieldDeclarationSyntax);

            var member = (MemberDeclarationSyntax)memberNode;

            var flags = member.GetModifierFlags();
            var containingType = member.FirstAncestorOrSelf<TypeDeclarationSyntax>();

            var result = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone;

            if ((flags & ModifierFlags.Abstract) != 0 || containingType?.Kind() == SyntaxKind.InterfaceDeclaration)
            {
                result |= EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract;
            }

            if ((flags & ModifierFlags.Virtual) != 0 || containingType?.Kind() == SyntaxKind.InterfaceDeclaration)
            {
                result |= EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual;
            }

            if ((flags & ModifierFlags.Override) != 0)
            {
                result |= EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride;
            }

            if ((flags & ModifierFlags.New) != 0)
            {
                result |= EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew;
            }

            if ((flags & ModifierFlags.Sealed) != 0)
            {
                result |= EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed;
            }

            return result;
        }

        public override SyntaxNode SetOverrideKind(SyntaxNode memberNode, EnvDTE80.vsCMOverrideKind kind)
        {
            Debug.Assert(memberNode is BaseMethodDeclarationSyntax ||
                         memberNode is BasePropertyDeclarationSyntax ||
                         memberNode is EventFieldDeclarationSyntax);

            // The legacy C# code model sets the MustImplement property here depending on whether the Abstract kind is set
            // TODO(DustinCa): VB implements MustImplement in terms of OverrideKind, should we do the same?
            memberNode = SetMustImplement(memberNode, (kind & EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract) != 0);

            var member = (MemberDeclarationSyntax)memberNode;
            var flags = member.GetModifierFlags();
            flags &= ~(ModifierFlags.Abstract | ModifierFlags.Virtual | ModifierFlags.Override | ModifierFlags.New | ModifierFlags.Sealed);

            if (member.IsParentKind(SyntaxKind.InterfaceDeclaration))
            {
                if ((kind & (EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride | EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)) != 0)
                {
                    throw Exceptions.ThrowEInvalidArg();
                }
                else if ((kind & (EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract | EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)) != 0)
                {
                    // Switch these flags off
                    kind &= ~(EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract | EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual);
                }
            }

            if (kind != EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
            {
                if ((kind & EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract) != 0)
                {
                    flags |= ModifierFlags.Abstract;
                }

                if ((kind & EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual) != 0)
                {
                    flags |= ModifierFlags.Virtual;
                }

                if ((kind & EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride) != 0)
                {
                    flags |= ModifierFlags.Override;
                }

                if ((kind & EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed) != 0)
                {
                    flags |= ModifierFlags.Sealed;
                }

                if ((kind & EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew) != 0)
                {
                    flags |= ModifierFlags.New;
                }
            }

            return member.UpdateModifiers(flags);
        }

        public override bool GetIsAbstract(SyntaxNode memberNode, ISymbol symbol)
        {
            Debug.Assert(memberNode is MemberDeclarationSyntax);

            var member = (MemberDeclarationSyntax)memberNode;

            return (member.GetModifierFlags() & ModifierFlags.Abstract) != 0;
        }

        public override SyntaxNode SetIsAbstract(SyntaxNode memberNode, bool value)
        {
            Debug.Assert(memberNode is MemberDeclarationSyntax);

            if (!(memberNode is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            var flags = member.GetModifierFlags();

            if (value)
            {
                flags |= ModifierFlags.Abstract;
            }
            else
            {
                flags &= ~ModifierFlags.Abstract;
            }

            return member.UpdateModifiers(flags);
        }

        public override bool GetIsConstant(SyntaxNode variableNode)
        {
            Debug.Assert(variableNode is MemberDeclarationSyntax ||
                         variableNode is VariableDeclaratorSyntax);

            if (variableNode is EnumMemberDeclarationSyntax)
            {
                return true;
            }

            if (!(GetNodeWithModifiers(variableNode) is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            // C# legacy Code Model returns true for readonly fields as well.
            return (member.GetModifierFlags() & (ModifierFlags.Const | ModifierFlags.ReadOnly)) != 0;
        }

        public override SyntaxNode SetIsConstant(SyntaxNode variableNode, bool value)
        {
            Debug.Assert(variableNode is MemberDeclarationSyntax);

            if (variableNode is EnumMemberDeclarationSyntax)
            {
                if (!value)
                {
                    throw Exceptions.ThrowEFail();
                }

                return variableNode;
            }

            if (!(variableNode is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            if (GetIsConstant(member) == value)
            {
                return member;
            }

            var flags = member.GetModifierFlags();

            if (value)
            {
                flags |= ModifierFlags.Const;
            }
            else
            {
                flags &= ~(ModifierFlags.Const | ModifierFlags.ReadOnly);
            }

            return member.UpdateModifiers(flags);
        }

        public override bool GetIsDefault(SyntaxNode propertyNode)
        {
            Debug.Assert(propertyNode is BasePropertyDeclarationSyntax);

            if (!(propertyNode is BasePropertyDeclarationSyntax property))
            {
                throw Exceptions.ThrowEFail();
            }

            return property.IsKind(SyntaxKind.IndexerDeclaration);
        }

        public override SyntaxNode SetIsDefault(SyntaxNode propertyNode, bool value)
        {
            // The C# legacy Code Model throws this specific exception rather than a COM exception.
            throw new InvalidOperationException();
        }

        public override bool GetIsGeneric(SyntaxNode memberNode)
        {
            Debug.Assert(memberNode is MemberDeclarationSyntax);

            if (!(GetNodeWithModifiers(memberNode) is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            return member.GetArity() > 0;
        }

        public override bool GetIsPropertyStyleEvent(SyntaxNode eventNode)
        {
            Debug.Assert(eventNode is EventFieldDeclarationSyntax ||
                         eventNode is EventDeclarationSyntax);

            return eventNode is EventDeclarationSyntax;
        }

        public override bool GetIsShared(SyntaxNode memberNode, ISymbol symbol)
        {
            Debug.Assert(memberNode is MemberDeclarationSyntax ||
                         memberNode is VariableDeclaratorSyntax);

            if (!(GetNodeWithModifiers(memberNode) is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            return (member.GetModifierFlags() & ModifierFlags.Static) != 0;
        }

        public override SyntaxNode SetIsShared(SyntaxNode memberNode, bool value)
        {
            Debug.Assert(memberNode is MemberDeclarationSyntax);

            if (!(memberNode is MemberDeclarationSyntax member))
            {
                throw Exceptions.ThrowEFail();
            }

            var flags = member.GetModifierFlags();

            if (value)
            {
                flags |= ModifierFlags.Static;
            }
            else
            {
                flags &= ~ModifierFlags.Static;
            }

            return member.UpdateModifiers(flags);
        }

        public override EnvDTE80.vsCMPropertyKind GetReadWrite(SyntaxNode memberNode)
        {
            Debug.Assert(memberNode is BasePropertyDeclarationSyntax);

            if (!(memberNode is BasePropertyDeclarationSyntax property))
            {
                throw Exceptions.ThrowEFail();
            }

            var hasGetter = property.AccessorList != null && property.AccessorList.Accessors.Any(SyntaxKind.GetAccessorDeclaration);
            var hasSetter = property.AccessorList != null && property.AccessorList.Accessors.Any(SyntaxKind.SetAccessorDeclaration);

            if (!hasGetter && !hasSetter)
            {
                var expressionBody = property.GetExpressionBody();
                if (expressionBody != null)
                {
                    hasGetter = true;
                }
            }

            if (hasGetter && hasSetter)
            {
                return EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite;
            }
            else if (hasSetter)
            {
                return EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly;
            }
            else if (hasGetter)
            {
                return EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly;
            }
            else
            {
                return EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite;
            }
        }

        public override SyntaxNode SetType(SyntaxNode node, ITypeSymbol typeSymbol)
        {
            Debug.Assert(node is MemberDeclarationSyntax ||
                         node is ParameterSyntax);

            TypeSyntax oldType;
            if (node is MemberDeclarationSyntax memberDeclaration)
            {
                oldType = memberDeclaration.GetMemberType();
            }
            else if (node is ParameterSyntax parameter)
            {
                oldType = parameter.Type;
            }
            else
            {
                throw Exceptions.ThrowEFail();
            }

            if (oldType == null)
            {
                throw Exceptions.ThrowEFail();
            }

            var typeName = typeSymbol.ToDisplayString(s_setTypeFormat);
            var newType = SyntaxFactory.ParseTypeName(typeName);

            return node.ReplaceNode(oldType, newType);
        }

        private Document Delete(Document document, VariableDeclaratorSyntax node)
        {
            var fieldDeclaration = node.FirstAncestorOrSelf<BaseFieldDeclarationSyntax>();

            // If we won't have anything left, then just delete the whole declaration
            if (fieldDeclaration.Declaration.Variables.Count == 1)
            {
                return Delete(document, fieldDeclaration);
            }
            else
            {
                var newFieldDeclaration = fieldDeclaration.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);

                return document.ReplaceNodeAsync(fieldDeclaration, newFieldDeclaration, CancellationToken.None)
                               .WaitAndGetResult_CodeModel(CancellationToken.None);
            }
        }

        private Document Delete(Document document, EnumMemberDeclarationSyntax node)
        {
            var enumDeclaration = (EnumDeclarationSyntax)node.Parent;
            var members = enumDeclaration.Members;
            var newMembers = members.Remove(node);
            var newEnumDeclaration = enumDeclaration.WithMembers(newMembers);

            // If we're removing the last enum member, we may need to move any trailing trivia
            // to the enum member that comes before it.
            var memberIndex = members.IndexOf(node);
            if (memberIndex == members.Count - 1 && newMembers.Count > 0)
            {
                var trailingTrivia = node.GetTrailingTrivia();
                var lastMember = newEnumDeclaration.Members.Last();
                newEnumDeclaration = newEnumDeclaration.ReplaceNode(lastMember, lastMember.WithTrailingTrivia(trailingTrivia));
            }

            return document.ReplaceNodeAsync(enumDeclaration, newEnumDeclaration, CancellationToken.None)
                           .WaitAndGetResult_CodeModel(CancellationToken.None);
        }

        private Document Delete(Document document, AttributeSyntax node)
        {
            var attributeList = node.FirstAncestorOrSelf<AttributeListSyntax>();

            // If we don't have anything left, then just delete the whole attribute list.
            if (attributeList.Attributes.Count == 1)
            {
                var text = document.GetTextAsync(CancellationToken.None)
                                   .WaitAndGetResult_CodeModel(CancellationToken.None);

                // Note that we want to keep all leading trivia and delete all trailing trivia.
                var deletionStart = attributeList.SpanStart;
                var deletionEnd = attributeList.FullSpan.End;

                text = text.Replace(TextSpan.FromBounds(deletionStart, deletionEnd), string.Empty);

                return document.WithText(text);
            }
            else
            {
                var newAttributeList = attributeList.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);

                return document.ReplaceNodeAsync(attributeList, newAttributeList, CancellationToken.None)
                               .WaitAndGetResult_CodeModel(CancellationToken.None);
            }
        }

        private Document Delete(Document document, AttributeArgumentSyntax node)
        {
            var argumentList = node.FirstAncestorOrSelf<AttributeArgumentListSyntax>();
            var newArgumentList = argumentList.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);

            return document.ReplaceNodeAsync(argumentList, newArgumentList, CancellationToken.None)
                           .WaitAndGetResult_CodeModel(CancellationToken.None);
        }

        private Document Delete(Document document, ParameterSyntax node)
        {
            var parameterList = node.FirstAncestorOrSelf<ParameterListSyntax>();
            var newParameterList = parameterList.RemoveNode(node, SyntaxRemoveOptions.KeepNoTrivia);

            return document.ReplaceNodeAsync(parameterList, newParameterList, CancellationToken.None)
                           .WaitAndGetResult_CodeModel(CancellationToken.None);
        }

        private Document DeleteMember(Document document, SyntaxNode node)
        {
            var text = document.GetTextAsync(CancellationToken.None)
                               .WaitAndGetResult_CodeModel(CancellationToken.None);

            // We want to delete all the leading trivia from the node back to,
            // but not including:
            //  * the first preprocessor directive
            //  - or -
            //  * the first comment after a white-space only line
            // We also want to delete all the trailing trivia

            var deletionEnd = node.FullSpan.End;

            var deletionStart = node.SpanStart;

            var contiguousEndOfLines = 0;
            foreach (var trivia in node.GetLeadingTrivia().Reverse())
            {
                if (trivia.IsDirective)
                {
                    break;
                }

                if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
                {
                    if (contiguousEndOfLines > 0)
                    {
                        break;
                    }
                    else
                    {
                        contiguousEndOfLines++;
                    }
                }
                else if (trivia.Kind() != SyntaxKind.WhitespaceTrivia)
                {
                    contiguousEndOfLines = 0;
                }

                deletionStart = trivia.FullSpan.Start;
            }

            text = text.Replace(TextSpan.FromBounds(deletionStart, deletionEnd), string.Empty);

            return document.WithText(text);
        }

        public override Document Delete(Document document, SyntaxNode node)
            => node.Kind() switch
            {
                SyntaxKind.VariableDeclarator => Delete(document, (VariableDeclaratorSyntax)node),
                SyntaxKind.EnumMemberDeclaration => Delete(document, (EnumMemberDeclarationSyntax)node),
                SyntaxKind.Attribute => Delete(document, (AttributeSyntax)node),
                SyntaxKind.AttributeArgument => Delete(document, (AttributeArgumentSyntax)node),
                SyntaxKind.Parameter => Delete(document, (ParameterSyntax)node),
                _ => DeleteMember(document, node),
            };

        public override string GetMethodXml(SyntaxNode node, SemanticModel semanticModel)
        {
            if (!(node is MethodDeclarationSyntax methodDeclaration))
            {
                throw Exceptions.ThrowEUnexpected();
            }

            return MethodXmlBuilder.Generate(methodDeclaration, semanticModel);
        }

        public override string GetInitExpression(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.EnumMemberDeclaration:
                    var enumMemberDeclaration = (EnumMemberDeclarationSyntax)node;
                    return enumMemberDeclaration.EqualsValue?.Value.ToString();
                case SyntaxKind.VariableDeclarator:
                    var variableDeclarator = (VariableDeclaratorSyntax)node;
                    return variableDeclarator.Initializer?.Value.ToString();
                case SyntaxKind.Parameter:
                    var parameter = (ParameterSyntax)node;
                    return parameter.Default?.Value.ToString();
                default:
                    throw Exceptions.ThrowEFail();
            }
        }

        public override SyntaxNode AddInitExpression(SyntaxNode node, string value)
        {
            switch (node.Kind())
            {
                case SyntaxKind.EnumMemberDeclaration:
                    {
                        var enumMemberDeclaration = (EnumMemberDeclarationSyntax)node;

                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return enumMemberDeclaration.WithEqualsValue(null);
                        }

                        var expression = SyntaxFactory.ParseExpression(value);
                        var equalsValueClause = enumMemberDeclaration.EqualsValue != null
                            ? enumMemberDeclaration.EqualsValue.WithValue(expression)
                            : SyntaxFactory.EqualsValueClause(expression);

                        return enumMemberDeclaration.WithEqualsValue(equalsValueClause);
                    }

                case SyntaxKind.VariableDeclarator:
                    {
                        var variableDeclarator = (VariableDeclaratorSyntax)node;

                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return variableDeclarator.WithInitializer(null);
                        }

                        var expression = SyntaxFactory.ParseExpression(value);
                        var equalsValueClause = variableDeclarator.Initializer != null
                            ? variableDeclarator.Initializer.WithValue(expression)
                            : SyntaxFactory.EqualsValueClause(expression);

                        return variableDeclarator.WithInitializer(equalsValueClause);
                    }

                case SyntaxKind.Parameter:
                    {
                        var parameter = (ParameterSyntax)node;

                        if (string.IsNullOrWhiteSpace(value))
                        {
                            return parameter.WithDefault(null);
                        }

                        var expression = SyntaxFactory.ParseExpression(value);

                        var equalsValueClause = parameter.Default != null
                            ? parameter.Default.WithValue(expression)
                            : SyntaxFactory.EqualsValueClause(expression);

                        return parameter.WithDefault(equalsValueClause);
                    }

                default:
                    throw Exceptions.ThrowEFail();
            }
        }

        public override CodeGenerationDestination GetDestination(SyntaxNode node)
        {
            return CSharpCodeGenerationHelpers.GetDestination(node);
        }

        protected override Accessibility GetDefaultAccessibility(SymbolKind targetSymbolKind, CodeGenerationDestination destination)
        {
            switch (targetSymbolKind)
            {
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    return Accessibility.Private;

                case SymbolKind.NamedType:
                    switch (destination)
                    {
                        case CodeGenerationDestination.ClassType:
                        case CodeGenerationDestination.EnumType:
                        case CodeGenerationDestination.InterfaceType:
                        case CodeGenerationDestination.StructType:
                            return Accessibility.Private;
                        default:
                            return Accessibility.Internal;
                    }

                default:
                    Debug.Fail("Invalid symbol kind: " + targetSymbolKind);
                    throw Exceptions.ThrowEFail();
            }
        }

        protected override ITypeSymbol GetTypeSymbolFromPartialName(string partialName, SemanticModel semanticModel, int position)
        {
            var parsedTypeName = SyntaxFactory.ParseTypeName(partialName);

            return semanticModel.GetSpeculativeTypeInfo(position, parsedTypeName, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
        }

        public override ITypeSymbol GetTypeSymbolFromFullName(string fullName, Compilation compilation)
        {
            ITypeSymbol typeSymbol = compilation.GetTypeByMetadataName(fullName);

            if (typeSymbol == null)
            {
                var parsedTypeName = SyntaxFactory.ParseTypeName(fullName);

                // Check to see if the name we parsed has any skipped text. If it does, don't bother trying to
                // speculatively bind it because we'll likely just get the wrong thing since we found a bunch
                // of non-sensical tokens.

                if (parsedTypeName.ContainsSkippedText)
                {
                    return null;
                }

                // If we couldn't get the name, we just grab the first tree in the compilation to
                // speculatively bind at position zero. However, if there *aren't* any trees, we fork the
                // compilation with an empty tree for the purposes of speculative binding.
                //
                // I'm a bad person.

                var tree = compilation.SyntaxTrees.FirstOrDefault();
                if (tree == null)
                {
                    tree = SyntaxFactory.ParseSyntaxTree("");
                    compilation = compilation.AddSyntaxTrees(tree);
                }

                var semanticModel = compilation.GetSemanticModel(tree);
                typeSymbol = semanticModel.GetSpeculativeTypeInfo(0, parsedTypeName, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
            }

            if (typeSymbol == null)
            {
                Debug.Fail("Could not find type: " + fullName);
                throw new ArgumentException();
            }

            return typeSymbol;
        }

        public override SyntaxNode CreateReturnDefaultValueStatement(ITypeSymbol type)
        {
            return SyntaxFactory.ReturnStatement(
                SyntaxFactory.DefaultExpression(
                    SyntaxFactory.ParseTypeName(type.ToDisplayString())));
        }

        protected override int GetAttributeIndexInContainer(SyntaxNode containerNode, Func<SyntaxNode, bool> predicate)
        {
            var attributes = GetAttributeNodes(containerNode).ToArray();

            var index = 0;
            while (index < attributes.Length)
            {
                var attribute = (AttributeSyntax)attributes[index];

                if (predicate(attribute))
                {
                    var attributeDeclaration = (AttributeListSyntax)attribute.Parent;

                    // If this attribute is part of a declaration with multiple attributes,
                    // make sure to return the index of the last attribute in the declaration.
                    if (attributeDeclaration.Attributes.Count > 1)
                    {
                        var indexOfAttributeInDeclaration = attributeDeclaration.Attributes.IndexOf(attribute);
                        return index + (attributeDeclaration.Attributes.Count - indexOfAttributeInDeclaration);
                    }

                    return index + 1;
                }

                index++;
            }

            return -1;
        }

        protected override int GetAttributeArgumentIndexInContainer(SyntaxNode containerNode, Func<SyntaxNode, bool> predicate)
        {
            var attributeArguments = GetAttributeArgumentNodes(containerNode).ToArray();

            for (var index = 0; index < attributeArguments.Length; index++)
            {
                if (predicate(attributeArguments[index]))
                {
                    return index + 1;
                }
            }

            return -1;
        }

        protected override int GetImportIndexInContainer(SyntaxNode containerNode, Func<SyntaxNode, bool> predicate)
        {
            var imports = GetImportNodes(containerNode).ToArray();

            for (var index = 0; index < imports.Length; index++)
            {
                if (predicate(imports[index]))
                {
                    return index + 1;
                }
            }

            return -1;
        }

        protected override int GetParameterIndexInContainer(SyntaxNode containerNode, Func<SyntaxNode, bool> predicate)
        {
            var parameters = GetParameterNodes(containerNode).ToArray();

            for (var index = 0; index < parameters.Length; index++)
            {
                if (predicate(parameters[index]))
                {
                    return index + 1;
                }
            }

            return -1;
        }

        protected override int GetMemberIndexInContainer(SyntaxNode containerNode, Func<SyntaxNode, bool> predicate)
        {
            var members = GetLogicalMemberNodes(containerNode).ToArray();

            var index = 0;
            while (index < members.Length)
            {
                var member = members[index];
                if (predicate(member))
                {
                    // If a variable declarator was specified, make sure we return
                    // the index of the last variable declarator in the parenting field declaration.
                    if (member.Kind() == SyntaxKind.VariableDeclarator)
                    {
                        var variableDeclarator = (VariableDeclaratorSyntax)member;
                        var variableDeclaration = (VariableDeclarationSyntax)member.Parent;
                        var indexOfDeclaratorInField = variableDeclaration.Variables.IndexOf(variableDeclarator);
                        return index + (variableDeclaration.Variables.Count - indexOfDeclaratorInField);
                    }

                    // Note: we always return the item *after* this index.
                    return index + 1;
                }

                index++;
            }

            return -1;
        }

        protected override SyntaxNode GetFieldFromVariableNode(SyntaxNode node)
        {
            return node.Kind() == SyntaxKind.VariableDeclarator
                ? node.FirstAncestorOrSelf<BaseFieldDeclarationSyntax>()
                : node;
        }

        protected override SyntaxNode GetVariableFromFieldNode(SyntaxNode finalNode)
        {
            // Work around the fact that code model really deals in terms of variable declarators
            return finalNode is BaseFieldDeclarationSyntax
                ? ((BaseFieldDeclarationSyntax)finalNode).Declaration.Variables.Single()
                : finalNode;
        }

        protected override SyntaxNode GetAttributeFromAttributeDeclarationNode(SyntaxNode node)
        {
            return node is AttributeListSyntax
                ? ((AttributeListSyntax)node).Attributes.First()
                : node;
        }

        protected override TextSpan GetSpanToFormat(SyntaxNode root, TextSpan span)
        {
            var startToken = root.FindToken(span.Start).GetPreviousToken();
            if (startToken.Kind() == SyntaxKind.OpenBraceToken)
            {
                startToken = startToken.GetPreviousToken();
            }

            var endToken = root.FindToken(span.End).GetNextToken();
            if (endToken.Kind() == SyntaxKind.CloseBraceToken)
            {
                endToken = endToken.GetPreviousToken();
            }

            startToken = GetTokenWithoutAnnotation(startToken, t => t.GetPreviousToken());
            endToken = GetTokenWithoutAnnotation(endToken, t => t.GetNextToken());

            return GetEncompassingSpan(root, startToken, endToken);
        }

        protected SyntaxNode InsertMemberNodeIntoContainerCore(int index, SyntaxNode member, SyntaxNode container)
        {
            if (container is CompilationUnitSyntax compilationUnit)
            {
                var newMembers = compilationUnit.Members.Insert(index, (MemberDeclarationSyntax)member);
                return compilationUnit.WithMembers(newMembers);
            }
            else if (container is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                var newMembers = namespaceDeclaration.Members.Insert(index, (MemberDeclarationSyntax)member);
                return namespaceDeclaration.WithMembers(newMembers);
            }
            else if (container is TypeDeclarationSyntax typeDeclaration)
            {
                var newMembers = typeDeclaration.Members.Insert(index, (MemberDeclarationSyntax)member);
                return typeDeclaration.WithMembers(newMembers);
            }
            else if (container is EnumDeclarationSyntax enumDeclaration)
            {
                // If we're inserting at the end of the list of enum members, we may need to strip the trailing
                // line from the last enum member and add it to the separator that comes after it.
                if (index > 0 && index == enumDeclaration.Members.Count)
                {
                    var lastMember = enumDeclaration.Members[index - 1];
                    var trailingTrivia = lastMember.GetTrailingTrivia();
                    enumDeclaration = enumDeclaration.ReplaceNode(lastMember, lastMember.WithTrailingTrivia(SyntaxTriviaList.Empty));

                    var newMembers = enumDeclaration.Members.Insert(index, (EnumMemberDeclarationSyntax)member);
                    enumDeclaration = enumDeclaration.WithMembers(newMembers);

                    var separator = enumDeclaration.Members.GetSeparator(index - 1);
                    return enumDeclaration.ReplaceToken(separator, separator.WithTrailingTrivia(trailingTrivia));
                }
                else
                {
                    var newMembers = enumDeclaration.Members.Insert(index, (EnumMemberDeclarationSyntax)member);
                    return enumDeclaration.WithMembers(newMembers);
                }
            }

            throw Exceptions.ThrowEFail();
        }

        private static MemberDeclarationSyntax GetMember(SyntaxNode container, int index)
        {
            if (container is CompilationUnitSyntax compilationUnit)
            {
                return compilationUnit.Members[index];
            }
            else if (container is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                return namespaceDeclaration.Members[index];
            }
            else if (container is TypeDeclarationSyntax typeDeclaration)
            {
                return typeDeclaration.Members[index];
            }
            else if (container is EnumDeclarationSyntax enumDeclaration)
            {
                return enumDeclaration.Members[index];
            }

            throw Exceptions.ThrowEFail();
        }

        private SyntaxNode EnsureAfterEndRegion(int index, SyntaxNode container)
        {
            // If the next token after our member has only whitespace and #endregion as leading
            // trivia, we'll move that to be leading trivia of our member.

            var newContainer = container;
            var newMember = GetMember(newContainer, index);

            var lastToken = newMember.GetLastToken();
            var nextToken = lastToken.GetNextToken();

            var triviaList = nextToken.LeadingTrivia;

            var lastNonWhitespaceTrivia = triviaList.LastOrDefault(trivia => !trivia.IsWhitespaceOrEndOfLine());
            if (lastNonWhitespaceTrivia.Kind() == SyntaxKind.EndRegionDirectiveTrivia)
            {
                newContainer = newContainer
                    .ReplaceToken(nextToken, nextToken.WithLeadingTrivia(SyntaxTriviaList.Empty));

                newMember = GetMember(newContainer, index);
                var firstToken = newMember.GetFirstToken();

                newContainer = newContainer
                    .ReplaceToken(firstToken, firstToken.WithLeadingTrivia(triviaList));
            }

            return newContainer;
        }

        protected override SyntaxNode InsertMemberNodeIntoContainer(int index, SyntaxNode member, SyntaxNode container)
        {
            var newContainer = InsertMemberNodeIntoContainerCore(index, member, container);

            newContainer = EnsureAfterEndRegion(index, newContainer);

            return newContainer;
        }

        protected override SyntaxNode InsertAttributeArgumentIntoContainer(int index, SyntaxNode attributeArgument, SyntaxNode container)
        {
            if (container is AttributeSyntax attribute)
            {
                var argumentList = attribute.ArgumentList;

                AttributeArgumentListSyntax newArgumentList;

                if (argumentList == null)
                {
                    newArgumentList = SyntaxFactory.AttributeArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            (AttributeArgumentSyntax)attributeArgument));
                }
                else
                {
                    var newArguments = argumentList.Arguments.Insert(index, (AttributeArgumentSyntax)attributeArgument);
                    newArgumentList = argumentList.WithArguments(newArguments);
                }

                return attribute.WithArgumentList(newArgumentList);
            }

            throw Exceptions.ThrowEFail();
        }

        protected override SyntaxNode InsertAttributeListIntoContainer(int index, SyntaxNode list, SyntaxNode container)
        {
            // If the attribute list is being inserted at the first index and the container is not the compilation unit, copy leading trivia
            // to the list that is being inserted.
            if (index == 0 && !(container is CompilationUnitSyntax))
            {
                var firstToken = container.GetFirstToken();
                if (firstToken.HasLeadingTrivia)
                {
                    var trivia = firstToken.LeadingTrivia;

                    container = container.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(SyntaxTriviaList.Empty));
                    list = list.WithLeadingTrivia(trivia);
                }
            }

            if (container is CompilationUnitSyntax compilationUnit)
            {
                var newAttributeLists = compilationUnit.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return compilationUnit.WithAttributeLists(newAttributeLists);
            }
            else if (container is EnumDeclarationSyntax enumDeclaration)
            {
                var newAttributeLists = enumDeclaration.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return enumDeclaration.WithAttributeLists(newAttributeLists);
            }
            else if (container is ClassDeclarationSyntax classDeclaration)
            {
                var newAttributeLists = classDeclaration.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return classDeclaration.WithAttributeLists(newAttributeLists);
            }
            else if (container is StructDeclarationSyntax structDeclaration)
            {
                var newAttributeLists = structDeclaration.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return structDeclaration.WithAttributeLists(newAttributeLists);
            }
            else if (container is InterfaceDeclarationSyntax interfaceDeclaration)
            {
                var newAttributeLists = interfaceDeclaration.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return interfaceDeclaration.WithAttributeLists(newAttributeLists);
            }
            else if (container is MethodDeclarationSyntax method)
            {
                var newAttributeLists = method.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return method.WithAttributeLists(newAttributeLists);
            }
            else if (container is OperatorDeclarationSyntax operationDeclaration)
            {
                var newAttributeLists = operationDeclaration.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return operationDeclaration.WithAttributeLists(newAttributeLists);
            }
            else if (container is ConversionOperatorDeclarationSyntax conversion)
            {
                var newAttributeLists = conversion.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return conversion.WithAttributeLists(newAttributeLists);
            }
            else if (container is ConstructorDeclarationSyntax constructor)
            {
                var newAttributeLists = constructor.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return constructor.WithAttributeLists(newAttributeLists);
            }
            else if (container is DestructorDeclarationSyntax destructor)
            {
                var newAttributeLists = destructor.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return destructor.WithAttributeLists(newAttributeLists);
            }
            else if (container is PropertyDeclarationSyntax property)
            {
                var newAttributeLists = property.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return property.WithAttributeLists(newAttributeLists);
            }
            else if (container is EventDeclarationSyntax eventDeclaration)
            {
                var newAttributeLists = eventDeclaration.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return eventDeclaration.WithAttributeLists(newAttributeLists);
            }
            else if (container is IndexerDeclarationSyntax indexer)
            {
                var newAttributeLists = indexer.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return indexer.WithAttributeLists(newAttributeLists);
            }
            else if (container is FieldDeclarationSyntax field)
            {
                var newAttributeLists = field.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return field.WithAttributeLists(newAttributeLists);
            }
            else if (container is EventFieldDeclarationSyntax eventFieldDeclaration)
            {
                var newAttributeLists = eventFieldDeclaration.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return eventFieldDeclaration.WithAttributeLists(newAttributeLists);
            }
            else if (container is DelegateDeclarationSyntax delegateDeclaration)
            {
                var newAttributeLists = delegateDeclaration.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return delegateDeclaration.WithAttributeLists(newAttributeLists);
            }
            else if (container is EnumMemberDeclarationSyntax member)
            {
                var newAttributeLists = member.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return member.WithAttributeLists(newAttributeLists);
            }
            else if (container is ParameterSyntax parameter)
            {
                var newAttributeLists = parameter.AttributeLists.Insert(index, (AttributeListSyntax)list);
                return parameter.WithAttributeLists(newAttributeLists);
            }
            else if (container is VariableDeclaratorSyntax ||
                     container is VariableDeclarationSyntax)
            {
                return InsertAttributeListIntoContainer(index, list, container.Parent);
            }

            throw Exceptions.ThrowEUnexpected();
        }

        protected override SyntaxNode InsertImportIntoContainer(int index, SyntaxNode importNode, SyntaxNode container)
        {
            var import = (UsingDirectiveSyntax)importNode;

            if (container is CompilationUnitSyntax compilationUnit)
            {
                var usingsList = compilationUnit.Usings.Insert(index, import);
                return compilationUnit.WithUsings(usingsList);
            }

            throw Exceptions.ThrowEUnexpected();
        }

        protected override SyntaxNode InsertParameterIntoContainer(int index, SyntaxNode parameter, SyntaxNode container)
        {
            if (container is BaseMethodDeclarationSyntax method)
            {
                var parameterList = method.ParameterList.Parameters.Insert(index, (ParameterSyntax)parameter);
                return method.WithParameterList(method.ParameterList.WithParameters(parameterList));
            }
            else if (container is IndexerDeclarationSyntax indexer)
            {
                var parameterList = indexer.ParameterList.Parameters.Insert(index, (ParameterSyntax)parameter);
                return indexer.WithParameterList(indexer.ParameterList.WithParameters(parameterList));
            }
            else if (container is DelegateDeclarationSyntax delegateDeclaration)
            {
                var parameterList = delegateDeclaration.ParameterList.Parameters.Insert(index, (ParameterSyntax)parameter);
                return delegateDeclaration.WithParameterList(delegateDeclaration.ParameterList.WithParameters(parameterList));
            }

            throw Exceptions.ThrowEUnexpected();
        }

        protected override bool IsCodeModelNode(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.CompilationUnit:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.EnumMemberDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.UsingDirective:
                    return true;

                default:
                    return false;
            }
        }

        public override bool IsNamespace(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.NamespaceDeclaration);
        }

        public override bool IsType(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.ClassDeclaration)
                || node.IsKind(SyntaxKind.InterfaceDeclaration)
                || node.IsKind(SyntaxKind.StructDeclaration)
                || node.IsKind(SyntaxKind.EnumDeclaration)
                || node.IsKind(SyntaxKind.DelegateDeclaration);
        }

        private static bool IsAutoImplementedProperty(PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration.IsParentKind(SyntaxKind.InterfaceDeclaration))
            {
                return false;
            }

            var modifierFlags = propertyDeclaration.GetModifierFlags();
            if ((modifierFlags & ModifierFlags.Abstract) != 0 ||
                (modifierFlags & ModifierFlags.Extern) != 0)
            {
                return false;
            }

            if (propertyDeclaration.AccessorList == null)
            {
                return false;
            }

            AccessorDeclarationSyntax getAccessor = null;
            AccessorDeclarationSyntax setAccessor = null;

            foreach (var accessor in propertyDeclaration.AccessorList.Accessors)
            {
                switch (accessor.Kind())
                {
                    case SyntaxKind.GetAccessorDeclaration:
                        if (getAccessor == null)
                        {
                            getAccessor = accessor;
                        }

                        break;
                    case SyntaxKind.SetAccessorDeclaration:
                        if (setAccessor == null)
                        {
                            setAccessor = accessor;
                        }

                        break;
                }
            }

            if (getAccessor == null || setAccessor == null)
            {
                return false;
            }

            return getAccessor.Body == null && setAccessor.Body == null;
        }

        private static bool IsExtensionMethod(MethodDeclarationSyntax methodDeclaration)
        {
            if (!methodDeclaration.IsParentKind(SyntaxKind.ClassDeclaration) ||
                !((ClassDeclarationSyntax)methodDeclaration.Parent).Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return false;
            }

            if (methodDeclaration.ParameterList == null ||
                methodDeclaration.ParameterList.Parameters.Count == 0)
            {
                return false;
            }

            return methodDeclaration.ParameterList.Parameters[0].Modifiers.Any(SyntaxKind.ThisKeyword);
        }

        private static bool IsPartialMethod(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
        }

        public override string[] GetFunctionExtenderNames()
        {
            return new[] { ExtenderNames.ExtensionMethod, ExtenderNames.PartialMethod };
        }

        public override object GetFunctionExtender(string name, SyntaxNode node, ISymbol symbol)
        {
            if (node == null || node.Kind() != SyntaxKind.MethodDeclaration ||
                symbol == null || symbol.Kind != SymbolKind.Method)
            {
                throw Exceptions.ThrowEUnexpected();
            }

            if (StringComparer.Ordinal.Equals(name, ExtenderNames.PartialMethod))
            {
                var methodDeclaration = (MethodDeclarationSyntax)node;
                var isPartial = IsPartialMethod(methodDeclaration);
                var isDeclaration = false;
                var hasOtherPart = false;

                if (isPartial)
                {
                    var methodSymbol = (IMethodSymbol)symbol;
                    isDeclaration = methodSymbol.PartialDefinitionPart == null;
                    hasOtherPart = isDeclaration
                        ? methodSymbol.PartialImplementationPart != null
                        : methodSymbol.PartialDefinitionPart != null;
                }

                return PartialMethodExtender.Create(isPartial, isDeclaration, hasOtherPart);
            }
            else if (StringComparer.Ordinal.Equals(name, ExtenderNames.ExtensionMethod))
            {
                var methodDeclaration = (MethodDeclarationSyntax)node;
                var isExtension = IsExtensionMethod(methodDeclaration);

                return ExtensionMethodExtender.Create(isExtension);
            }

            throw Exceptions.ThrowEFail();
        }

        public override string[] GetPropertyExtenderNames()
        {
            return new[] { ExtenderNames.AutoImplementedProperty };
        }

        public override object GetPropertyExtender(string name, SyntaxNode node, ISymbol symbol)
        {
            if (node == null || node.Kind() != SyntaxKind.PropertyDeclaration ||
                symbol == null || symbol.Kind != SymbolKind.Property)
            {
                throw Exceptions.ThrowEUnexpected();
            }

            if (StringComparer.Ordinal.Equals(name, ExtenderNames.AutoImplementedProperty))
            {
                var propertyDeclaration = (PropertyDeclarationSyntax)node;
                var isAutoImplemented = IsAutoImplementedProperty(propertyDeclaration);

                return AutoImplementedPropertyExtender.Create(isAutoImplemented);
            }

            throw Exceptions.ThrowEFail();
        }

        public override string[] GetExternalTypeExtenderNames()
        {
            return new[] { ExtenderNames.ExternalLocation };
        }

        public override object GetExternalTypeExtender(string name, string externalLocation)
        {
            Debug.Assert(externalLocation != null);

            if (StringComparer.Ordinal.Equals(name, ExtenderNames.ExternalLocation))
            {
                return CodeTypeLocationExtender.Create(externalLocation);
            }

            throw Exceptions.ThrowEFail();
        }

        public override string[] GetTypeExtenderNames()
        {
            return Array.Empty<string>();
        }

        public override object GetTypeExtender(string name, AbstractCodeType symbol)
        {
            throw Exceptions.ThrowEFail();
        }

        protected override bool AddBlankLineToMethodBody(SyntaxNode node, SyntaxNode newNode)
        {
            return node is MethodDeclarationSyntax methodDeclaration
                && methodDeclaration.Body == null
                && newNode is MethodDeclarationSyntax newMethodDeclaration
                && newMethodDeclaration.Body != null;
        }

        private static TypeDeclarationSyntax InsertIntoBaseList(TypeDeclarationSyntax typeDeclaration, ITypeSymbol typeSymbol, SemanticModel semanticModel, int insertionIndex)
        {
            var position = typeDeclaration.SpanStart;
            var identifier = typeDeclaration.Identifier;
            if (identifier.HasTrailingTrivia)
            {
                typeDeclaration = typeDeclaration.WithIdentifier(
                    identifier.WithTrailingTrivia(identifier.TrailingTrivia.SkipWhile(t => t.IsWhitespaceOrEndOfLine())));
            }

            var typeName = SyntaxFactory.ParseTypeName(typeSymbol.ToMinimalDisplayString(semanticModel, position));
            var baseList = typeDeclaration.BaseList != null
                ? typeDeclaration.BaseList.WithTypes(typeDeclaration.BaseList.Types.Insert(insertionIndex, SyntaxFactory.SimpleBaseType(typeName)))
                : SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList((BaseTypeSyntax)SyntaxFactory.SimpleBaseType(typeName)));

            return typeDeclaration.WithBaseList(baseList);
        }

        public override bool IsValidBaseType(SyntaxNode node, ITypeSymbol typeSymbol)
        {
            if (node.IsKind(SyntaxKind.ClassDeclaration))
            {
                return typeSymbol.TypeKind == TypeKind.Class;
            }
            else if (node.IsKind(SyntaxKind.InterfaceDeclaration))
            {
                return typeSymbol.TypeKind == TypeKind.Interface;
            }

            return false;
        }

        public override SyntaxNode AddBase(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel, int? position)
        {
            if (!node.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration))
            {
                throw Exceptions.ThrowEFail();
            }

            var typeDeclaration = (TypeDeclarationSyntax)node;
            var baseCount = typeDeclaration.BaseList != null
                ? typeDeclaration.BaseList.Types.Count
                : 0;

            int insertionIndex;
            if (typeDeclaration.IsKind(SyntaxKind.ClassDeclaration))
            {
                insertionIndex = 0;
            }
            else if (position != null)
            {
                insertionIndex = position.Value;
                if (insertionIndex > baseCount)
                {
                    throw Exceptions.ThrowEInvalidArg();
                }
            }
            else
            {
                insertionIndex = baseCount;
            }

            return InsertIntoBaseList(typeDeclaration, typeSymbol, semanticModel, insertionIndex);
        }

        public override SyntaxNode RemoveBase(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel)
        {
            if (!node.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration))
            {
                throw Exceptions.ThrowEFail();
            }

            var typeDeclaration = (TypeDeclarationSyntax)node;
            if (typeDeclaration.BaseList == null ||
                typeDeclaration.BaseList.Types.Count == 0)
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            var isFirst = true;
            BaseTypeSyntax baseType = null;

            foreach (var type in typeDeclaration.BaseList.Types)
            {
                if (!isFirst && node.IsKind(SyntaxKind.ClassDeclaration))
                {
                    break;
                }

                var typeInfo = semanticModel.GetTypeInfo(type.Type, CancellationToken.None);
                if (typeInfo.Type != null &&
                    typeInfo.Type.Equals(typeSymbol))
                {
                    baseType = type;
                    break;
                }

                isFirst = false;
            }

            if (baseType == null)
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            var newTypes = typeDeclaration.BaseList.Types.Remove(baseType);
            var newBaseList = typeDeclaration.BaseList.WithTypes(newTypes);
            if (newBaseList.Types.Count == 0)
            {
                newBaseList = null;
            }

            return typeDeclaration.WithBaseList(newBaseList);
        }

        public override bool IsValidInterfaceType(SyntaxNode node, ITypeSymbol typeSymbol)
        {
            if (node.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration))
            {
                return typeSymbol.TypeKind == TypeKind.Interface;
            }

            return false;
        }

        public override SyntaxNode AddImplementedInterface(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel, int? position)
        {
            if (!node.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration))
            {
                throw Exceptions.ThrowEFail();
            }

            if (typeSymbol.Kind != SymbolKind.NamedType ||
                typeSymbol.TypeKind != TypeKind.Interface)
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            var typeDeclaration = (TypeDeclarationSyntax)node;
            var baseCount = typeDeclaration.BaseList != null
                ? typeDeclaration.BaseList.Types.Count
                : 0;

            int insertionIndex;
            if (position != null)
            {
                insertionIndex = position.Value;
                if (insertionIndex > baseCount)
                {
                    throw Exceptions.ThrowEInvalidArg();
                }
            }
            else
            {
                insertionIndex = baseCount;
            }

            return InsertIntoBaseList(typeDeclaration, typeSymbol, semanticModel, insertionIndex);
        }

        public override SyntaxNode RemoveImplementedInterface(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel)
        {
            if (!node.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration))
            {
                throw Exceptions.ThrowEFail();
            }

            var typeDeclaration = (TypeDeclarationSyntax)node;
            if (typeDeclaration.BaseList == null ||
                typeDeclaration.BaseList.Types.Count == 0)
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            BaseTypeSyntax baseType = null;
            foreach (var type in typeDeclaration.BaseList.Types)
            {
                var typeInfo = semanticModel.GetTypeInfo(type.Type, CancellationToken.None);
                if (typeInfo.Type != null &&
                    typeInfo.Type.Equals(typeSymbol))
                {
                    baseType = type;
                    break;
                }
            }

            if (baseType == null)
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            var newTypes = typeDeclaration.BaseList.Types.Remove(baseType);
            var newBaseList = typeDeclaration.BaseList.WithTypes(newTypes);
            if (newBaseList.Types.Count == 0)
            {
                newBaseList = null;
            }

            return typeDeclaration.WithBaseList(newBaseList);
        }
    }
}
