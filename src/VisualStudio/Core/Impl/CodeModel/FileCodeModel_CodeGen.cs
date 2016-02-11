// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// Implementations of EnvDTE.FileCodeModel for both languages.
    /// </summary>
    public sealed partial class FileCodeModel
    {
        private SyntaxNode InsertAttribute(SyntaxNode containerNode, SyntaxNode attributeNode, int insertionIndex)
        {
            return PerformEdit(document =>
            {
                Document newDocument;
                var resultNode = CodeModelService.InsertAttribute(
                    document, IsBatchOpen, insertionIndex, containerNode, attributeNode, CancellationToken.None, out newDocument);

                return Tuple.Create(resultNode, newDocument);
            });
        }

        private SyntaxNode InsertAttributeArgument(SyntaxNode containerNode, SyntaxNode attributeArgumentNode, int insertionIndex)
        {
            return PerformEdit(document =>
            {
                Document newDocument;
                var resultNode = CodeModelService.InsertAttributeArgument(
                    document, IsBatchOpen, insertionIndex, containerNode, attributeArgumentNode, CancellationToken.None, out newDocument);

                return Tuple.Create(resultNode, newDocument);
            });
        }

        private SyntaxNode InsertImport(SyntaxNode containerNode, SyntaxNode importNode, int insertionIndex)
        {
            return PerformEdit(document =>
            {
                Document newDocument;
                var resultNode = CodeModelService.InsertImport(
                    document, IsBatchOpen, insertionIndex, containerNode, importNode, CancellationToken.None, out newDocument);

                return Tuple.Create(resultNode, newDocument);
            });
        }

        private SyntaxNode InsertMember(SyntaxNode containerNode, SyntaxNode memberNode, int insertionIndex)
        {
            return PerformEdit(document =>
            {
                Document newDocument;
                var resultNode = CodeModelService.InsertMember(
                    document, IsBatchOpen, insertionIndex, containerNode, memberNode, CancellationToken.None, out newDocument);

                OnBeforeCodeElementCreated(resultNode);

                return Tuple.Create(resultNode, newDocument);
            });
        }

        private SyntaxNode InsertParameter(SyntaxNode containerNode, SyntaxNode parameterNode, int insertionIndex)
        {
            return PerformEdit(document =>
            {
                Document newDocument;
                var resultNode = CodeModelService.InsertParameter(
                    document, IsBatchOpen, insertionIndex, containerNode, parameterNode, CancellationToken.None, out newDocument);

                return Tuple.Create(resultNode, newDocument);
            });
        }

        private EnvDTE.CodeElement CreateInternalCodeMember(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node)
        {
            var element = CodeModelService.CreateInternalCodeElement(state, fileCodeModel, node);
            if (IsBatchOpen)
            {
                var codeElement = ComAggregate.TryGetManagedObject<AbstractKeyedCodeElement>(element);
                if (codeElement != null)
                {
                    _batchElements.Add(codeElement);
                }
            }

            return element;
        }

        private void UpdateNode(SyntaxNode node, SyntaxNode updatedNode)
        {
            PerformEdit(document =>
            {
                return CodeModelService.UpdateNode(document, node, updatedNode, CancellationToken.None);
            });
        }

        private static object[] GetValidArray(object itemOrArray, bool allowMultipleElements)
        {
            // TODO(DustinCa): Check VB's behavior when a bad array is passed.
            var result = new List<object>();

            if (itemOrArray != null && itemOrArray != DBNull.Value && itemOrArray != Type.Missing)
            {
                if (itemOrArray is Array)
                {
                    var realArray = (Array)itemOrArray;

                    if (realArray.Rank != 1)
                    {
                        throw Exceptions.ThrowEInvalidArg();
                    }

                    // TODO(DustinCa): This is duplicating the C# source. Is it true that non-zero based arrays are supported?
                    var lowerBound = realArray.GetLowerBound(0);
                    var upperBound = realArray.GetUpperBound(0);

                    if (!allowMultipleElements && upperBound > lowerBound)
                    {
                        throw Exceptions.ThrowEInvalidArg();
                    }

                    for (var i = lowerBound; i <= upperBound; i++)
                    {
                        var item = realArray.GetValue(i);

                        if (item != null && item != DBNull.Value)
                        {
                            result.Add(item);
                        }
                    }
                }
                else
                {
                    result.Add(itemOrArray);
                }
            }

            return result.ToArray();
        }

        internal EnvDTE80.CodeAttributeArgument AddAttributeArgument(SyntaxNode containerNode, string name, string value, object position)
        {
            var attributeArgumentNode = CodeModelService.CreateAttributeArgumentNode(CodeModelService.GetUnescapedName(name), value);
            var insertionIndex = CodeModelService.PositionVariantToAttributeArgumentInsertionIndex(position, containerNode, fileCodeModel: this);

            var newNode = InsertAttributeArgument(containerNode, attributeArgumentNode, insertionIndex);

            return (EnvDTE80.CodeAttributeArgument)CodeModelService.CreateInternalCodeElement(this.State, fileCodeModel: this, node: newNode);
        }

        internal EnvDTE.CodeAttribute AddAttribute(SyntaxNode containerNode, string name, string value, object position, string target = null)
        {
            containerNode = CodeModelService.GetNodeWithAttributes(containerNode);
            var attributeNode = CodeModelService.CreateAttributeNode(CodeModelService.GetUnescapedName(name), value, target);
            var insertionIndex = CodeModelService.PositionVariantToAttributeInsertionIndex(position, containerNode, fileCodeModel: this);

            var newNode = InsertAttribute(containerNode, attributeNode, insertionIndex);

            return (EnvDTE.CodeAttribute)CodeModelService.CreateInternalCodeElement(this.State, fileCodeModel: this, node: newNode);
        }

        internal EnvDTE.CodeParameter AddParameter(EnvDTE.CodeElement parent, SyntaxNode containerNode, string name, object type, object position)
        {
            var typeSymbol = CodeModelService.GetTypeSymbol(type, this.GetSemanticModel(), containerNode.SpanStart);
            var typeName = typeSymbol.GetEscapedFullName();

            var parameterNode = CodeModelService.CreateParameterNode(CodeModelService.GetUnescapedName(name), typeName);
            var insertionIndex = CodeModelService.PositionVariantToParameterInsertionIndex(position, containerNode, fileCodeModel: this);

            var newNode = InsertParameter(containerNode, parameterNode, insertionIndex);

            // Since parameters form part of the NodeKey for functions, delegates, and indexers,
            // creating a CodeParameter hooked up to the correct parent is a little tricky. After
            // the call to InsertParameter, the syntax tree has been updated, but not the NodeKey
            // map or the NodeKey in the parent CodeParameter. If we delegate the creation of the
            // CodeParameter to CodeModelService.CreateInternalCodeElement, it will attempt to
            // look up an element in the NodeKey map based on the new syntax tree. This will fail,
            // causing it to create a new, duplicate element for the parent. Later, when we
            // reacquire the NodeKeys, the original element will get the proper NodeKey, while the
            // duplicate will be updated to a meaningless NodeKey. Since the duplicate is the one
            // being used by the CodeParameter, most operations on it will then fail.
            // Instead, we need to have the parent passed in to us.
            var parentObj = ComAggregate.GetManagedObject<AbstractCodeMember>(parent);
            return CodeParameter.Create(this.State, parentObj, CodeModelService.GetParameterName(newNode));
        }

        internal EnvDTE.CodeClass AddClass(SyntaxNode containerNode, string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
        {
            var containerNodePosition = containerNode.SpanStart;
            var semanticModel = GetSemanticModel();

            var baseArray = GetValidArray(bases, allowMultipleElements: false);
            Debug.Assert(baseArray.Length == 0 || baseArray.Length == 1);

            var baseTypeSymbol = baseArray.Length == 1
                ? (INamedTypeSymbol)CodeModelService.GetTypeSymbol(baseArray[0], semanticModel, containerNodePosition)
                : null;

            var implementedInterfaceArray = GetValidArray(implementedInterfaces, allowMultipleElements: true);

            var implementedInterfaceSymbols = Array.ConvertAll(implementedInterfaceArray,
                i => (INamedTypeSymbol)CodeModelService.GetTypeSymbol(i, semanticModel, containerNodePosition));

            var newType = CreateTypeDeclaration(
                containerNode,
                TypeKind.Class,
                CodeModelService.GetUnescapedName(name),
                access,
                baseType: baseTypeSymbol,
                implementedInterfaces: implementedInterfaceSymbols);

            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newType = InsertMember(containerNode, newType, insertionIndex);

            return (EnvDTE.CodeClass)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newType);
        }

        internal EnvDTE.CodeDelegate AddDelegate(SyntaxNode containerNode, string name, object type, object position, EnvDTE.vsCMAccess access)
        {
            var containerNodePosition = containerNode.SpanStart;
            var semanticModel = GetSemanticModel();

            var returnType = (INamedTypeSymbol)CodeModelService.GetTypeSymbol(type, semanticModel, containerNodePosition);

            var newType = CreateDelegateTypeDeclaration(containerNode, CodeModelService.GetUnescapedName(name), access, returnType);
            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newType = InsertMember(containerNode, newType, insertionIndex);

            return (EnvDTE.CodeDelegate)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newType);
        }

        internal EnvDTE.CodeEnum AddEnum(SyntaxNode containerNode, string name, object position, object bases, EnvDTE.vsCMAccess access)
        {
            // TODO(DustinCa): "bases" is ignored in C# code model. Need to check VB.

            var newType = CreateTypeDeclaration(containerNode, TypeKind.Enum, name, access);
            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newType = InsertMember(containerNode, newType, insertionIndex);

            return (EnvDTE.CodeEnum)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newType);
        }

        public EnvDTE.CodeVariable AddEnumMember(SyntaxNode containerNode, string name, object value, object position)
        {
            if (value != null && !(value is string))
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            var containerNodePosition = containerNode.SpanStart;
            var semanticModel = GetSemanticModel();

            var type = semanticModel.GetDeclaredSymbol(containerNode) as ITypeSymbol;
            var newField = CreateFieldDeclaration(containerNode, CodeModelService.GetUnescapedName(name), EnvDTE.vsCMAccess.vsCMAccessPublic, type);
            if (value != null)
            {
                newField = CodeModelService.AddInitExpression(newField, (string)value);
            }

            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newField = InsertMember(containerNode, newField, insertionIndex);

            return (EnvDTE.CodeVariable)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newField);
        }

        public EnvDTE80.CodeEvent AddEvent(SyntaxNode containerNode, string name, string fullDelegateName, bool createPropertyStyleEvent, object position, EnvDTE.vsCMAccess access)
        {
            var containerNodePosition = containerNode.SpanStart;
            var semanticModel = GetSemanticModel();

            var eventType = (INamedTypeSymbol)CodeModelService.GetTypeSymbol(fullDelegateName, semanticModel, containerNodePosition);

            var newEvent = CreateEventDeclaration(containerNode, CodeModelService.GetUnescapedName(name), access, eventType, createPropertyStyleEvent);
            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newEvent = InsertMember(containerNode, newEvent, insertionIndex);

            return (EnvDTE80.CodeEvent)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newEvent);
        }

        internal EnvDTE.CodeFunction AddFunction(SyntaxNode containerNode, string name, EnvDTE.vsCMFunction kind, object type, object position, EnvDTE.vsCMAccess access)
        {
            kind = CodeModelService.ValidateFunctionKind(containerNode, kind, name);

            SyntaxNode newMember;

            if (kind == EnvDTE.vsCMFunction.vsCMFunctionSub ||
                kind == EnvDTE.vsCMFunction.vsCMFunctionFunction)
            {
                var containerNodePosition = containerNode.SpanStart;
                var semanticModel = GetSemanticModel();
                var returnType = kind == EnvDTE.vsCMFunction.vsCMFunctionFunction
                    ? CodeModelService.GetTypeSymbol(type, semanticModel, containerNodePosition)
                    : semanticModel.Compilation.GetSpecialType(SpecialType.System_Void);

                newMember = CreateMethodDeclaration(containerNode, CodeModelService.GetUnescapedName(name), access, returnType);
            }
            else if (kind == EnvDTE.vsCMFunction.vsCMFunctionConstructor)
            {
                newMember = CreateConstructorDeclaration(containerNode, CodeModelService.GetUnescapedName(name), access);
            }
            else
            {
                newMember = CreateDestructorDeclaration(containerNode, CodeModelService.GetUnescapedName(name));
            }

            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newMember = InsertMember(containerNode, newMember, insertionIndex);

            return (EnvDTE.CodeFunction)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newMember);
        }

        internal EnvDTE80.CodeImport AddImport(SyntaxNode containerNode, string name, object position, string alias)
        {
            var importNode = CodeModelService.CreateImportNode(CodeModelService.GetUnescapedName(name), alias);
            var insertionIndex = CodeModelService.PositionVariantToImportInsertionIndex(position, containerNode, fileCodeModel: this);

            var newNode = InsertImport(containerNode, importNode, insertionIndex);

            return (EnvDTE80.CodeImport)CodeModelService.CreateInternalCodeElement(this.State, fileCodeModel: this, node: newNode);
        }

        internal EnvDTE.CodeInterface AddInterface(SyntaxNode containerNode, string name, object position, object bases, EnvDTE.vsCMAccess access)
        {
            var containerNodePosition = containerNode.SpanStart;
            var semanticModel = GetSemanticModel();

            var implementedInterfaceArray = GetValidArray(bases, allowMultipleElements: true);

            var implementedInterfaceSymbols = Array.ConvertAll(implementedInterfaceArray,
                i => (INamedTypeSymbol)CodeModelService.GetTypeSymbol(i, semanticModel, containerNodePosition));

            var newType = CreateTypeDeclaration(
                containerNode,
                TypeKind.Interface,
                CodeModelService.GetUnescapedName(name),
                access,
                implementedInterfaces: implementedInterfaceSymbols);

            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newType = InsertMember(containerNode, newType, insertionIndex);

            return (EnvDTE.CodeInterface)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newType);
        }

        internal EnvDTE.CodeNamespace AddNamespace(SyntaxNode containerNode, string name, object position)
        {
            var newNamespace = CreateNamespaceDeclaration(containerNode, name);
            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newNamespace = InsertMember(containerNode, newNamespace, insertionIndex);

            return (EnvDTE.CodeNamespace)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newNamespace);
        }

        internal EnvDTE.CodeProperty AddProperty(SyntaxNode containerNode, string getterName, string putterName, object type, object position, EnvDTE.vsCMAccess access)
        {
            bool isGetterPresent = !string.IsNullOrEmpty(getterName);
            bool isPutterPresent = !string.IsNullOrEmpty(putterName);

            if ((!isGetterPresent && !isPutterPresent) ||
                (isGetterPresent && isPutterPresent && getterName != putterName))
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            var containerNodePosition = containerNode.SpanStart;
            var semanticModel = GetSemanticModel();

            var propertyType = CodeModelService.GetTypeSymbol(type, semanticModel, containerNodePosition);
            var newProperty = CreatePropertyDeclaration(containerNode,
                                                        CodeModelService.GetUnescapedName(isGetterPresent ? getterName : putterName),
                                                        isGetterPresent,
                                                        isPutterPresent,
                                                        access, propertyType);
            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newProperty = InsertMember(containerNode, newProperty, insertionIndex);

            return (EnvDTE.CodeProperty)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newProperty);
        }

        internal EnvDTE.CodeStruct AddStruct(SyntaxNode containerNode, string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
        {
            // TODO(DustinCa): Old C# code base doesn't even check bases for validity -- does VB?

            var containerNodePosition = containerNode.SpanStart;
            var semanticModel = GetSemanticModel();

            var implementedInterfaceArray = GetValidArray(bases, allowMultipleElements: true);

            var implementedInterfaceSymbols = Array.ConvertAll(implementedInterfaceArray,
                i => (INamedTypeSymbol)CodeModelService.GetTypeSymbol(i, semanticModel, containerNodePosition));

            var newType = CreateTypeDeclaration(
                containerNode,
                TypeKind.Struct,
                CodeModelService.GetUnescapedName(name),
                access,
                implementedInterfaces: implementedInterfaceSymbols);

            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newType = InsertMember(containerNode, newType, insertionIndex);

            return (EnvDTE.CodeStruct)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newType);
        }

        public EnvDTE.CodeVariable AddVariable(SyntaxNode containerNode, string name, object type, object position, EnvDTE.vsCMAccess access)
        {
            var containerNodePosition = containerNode.SpanStart;
            var semanticModel = GetSemanticModel();

            var fieldType = CodeModelService.GetTypeSymbol(type, semanticModel, containerNodePosition);
            var newField = CreateFieldDeclaration(containerNode, CodeModelService.GetUnescapedName(name), access, fieldType);
            var insertionIndex = CodeModelService.PositionVariantToMemberInsertionIndex(position, containerNode, fileCodeModel: this);

            newField = InsertMember(containerNode, newField, insertionIndex);

            return (EnvDTE.CodeVariable)CreateInternalCodeMember(this.State, fileCodeModel: this, node: newField);
        }

        internal void UpdateAccess(SyntaxNode node, EnvDTE.vsCMAccess access)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetAccess(node, access);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateAttributeTarget(SyntaxNode node, string value)
        {
            node = CodeModelService.GetAttributeTargetNode(node);
            var updatedNode = CodeModelService.SetAttributeTarget(node, value);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateAttributeValue(SyntaxNode node, string value)
        {
            var updatedNode = CodeModelService.SetAttributeValue(node, value);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateCanOverride(SyntaxNode node, bool value)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetCanOverride(node, value);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateClassKind(SyntaxNode node, EnvDTE80.vsCMClassKind kind)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetClassKind(node, kind);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateComment(SyntaxNode node, string value)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetComment(node, value);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateConstKind(SyntaxNode node, EnvDTE80.vsCMConstKind kind)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetConstKind(node, kind);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateDataTypeKind(SyntaxNode node, EnvDTE80.vsCMDataTypeKind kind)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetDataTypeKind(node, kind);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateDocComment(SyntaxNode node, string value)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetDocComment(node, value);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateInheritanceKind(SyntaxNode node, EnvDTE80.vsCMInheritanceKind kind)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetInheritanceKind(node, kind);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateIsConstant(SyntaxNode node, bool value)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetIsConstant(node, value);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateIsDefault(SyntaxNode node, bool value)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetIsDefault(node, value);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateIsShared(SyntaxNode node, bool value)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetIsShared(node, value);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateMustImplement(SyntaxNode node, bool value)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetMustImplement(node, value);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateName(SyntaxNode node, string name)
        {
            node = CodeModelService.GetNodeWithName(node);
            var updatedNode = CodeModelService.SetName(node, name);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateOverrideKind(SyntaxNode node, EnvDTE80.vsCMOverrideKind kind)
        {
            node = CodeModelService.GetNodeWithModifiers(node);
            var updatedNode = CodeModelService.SetOverrideKind(node, kind);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateParameterKind(SyntaxNode node, EnvDTE80.vsCMParameterKind kind)
        {
            var updatedNode = CodeModelService.SetParameterKind(node, kind);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateInitExpression(SyntaxNode node, string initExpression)
        {
            node = CodeModelService.GetNodeWithInitializer(node);
            var updatedNode = CodeModelService.AddInitExpression(node, initExpression);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void UpdateType(SyntaxNode node, EnvDTE.CodeTypeRef codeTypeRef)
        {
            node = CodeModelService.GetNodeWithType(node);
            var typeSymbol = codeTypeRef != null
                ? CodeModelService.GetTypeSymbolFromFullName(codeTypeRef.AsFullName, GetCompilation())
                : null;

            var updatedNode = CodeModelService.SetType(node, typeSymbol);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        private static int? GetRealPosition(object position)
        {
            int? realPosition;

            if (position == Type.Missing)
            {
                realPosition = null;
            }
            else if (position == null)
            {
                realPosition = 0;
            }
            else if (position is int)
            {
                realPosition = (int)position;

                // -1 means "add to the end". We'll null for that.
                if (realPosition == -1)
                {
                    realPosition = null;
                }
            }
            else
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            return realPosition;
        }

        internal void AddBase(SyntaxNode node, object @base, object position = null)
        {
            var semanticModel = GetSemanticModel();
            var typeSymbol = CodeModelService.GetTypeSymbol(@base, semanticModel, node.SpanStart);

            // If a CodeType or CodeTypeRef was specified, we need to verify that a
            // valid base type was specified (i.e. class for class, interface for interface).
            if (CodeModelInterop.CanChangedVariantType(@base, VarEnum.VT_UNKNOWN) &&
                !CodeModelService.IsValidBaseType(node, typeSymbol))
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            int? realPosition = GetRealPosition(position);

            var updatedNode = CodeModelService.AddBase(node, typeSymbol, semanticModel, realPosition);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal void RemoveBase(SyntaxNode node, object element)
        {
            var semanticModel = GetSemanticModel();
            var typeSymbol = CodeModelService.GetTypeSymbol(element, semanticModel, node.SpanStart);

            var updatedNode = CodeModelService.RemoveBase(node, typeSymbol, semanticModel);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }

        internal string AddImplementedInterface(SyntaxNode node, object @base, object position = null)
        {
            var semanticModel = GetSemanticModel();
            var typeSymbol = CodeModelService.GetTypeSymbol(@base, semanticModel, node.SpanStart);

            // If a CodeType or CodeTypeRef was specified, we need to verify that a
            // valid base type was specified (i.e. an interface for a struct or class).
            if (CodeModelInterop.CanChangedVariantType(@base, VarEnum.VT_UNKNOWN) &&
                !CodeModelService.IsValidInterfaceType(node, typeSymbol))
            {
                throw Exceptions.ThrowEInvalidArg();
            }

            int? realPosition = GetRealPosition(position);

            var updatedNode = CodeModelService.AddImplementedInterface(node, typeSymbol, semanticModel, realPosition);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }

            return typeSymbol.Name;
        }

        internal void RemoveImplementedInterface(SyntaxNode node, object element)
        {
            var semanticModel = GetSemanticModel();
            var typeSymbol = CodeModelService.GetTypeSymbol(element, semanticModel, node.SpanStart);

            var updatedNode = CodeModelService.RemoveImplementedInterface(node, typeSymbol, semanticModel);

            if (node != updatedNode)
            {
                UpdateNode(node, updatedNode);
            }
        }
    }
}
