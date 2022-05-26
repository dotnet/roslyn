// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal interface ICodeModelService : ICodeModelNavigationPointService
    {
        /// <summary>
        /// Retrieves the Option nodes (i.e. VB Option statements) parented
        /// by the given node.
        /// </summary>
        IEnumerable<SyntaxNode> GetOptionNodes(SyntaxNode parent);

        /// <summary>
        /// Retrieves the import nodes (e.g. using/Import directives) parented
        /// by the given node.
        /// </summary>
        IEnumerable<SyntaxNode> GetImportNodes(SyntaxNode parent);

        /// <summary>
        /// Retrieves the attributes parented or owned by the given node.
        /// </summary>
        IEnumerable<SyntaxNode> GetAttributeNodes(SyntaxNode parent);

        /// <summary>
        /// Retrieves the attribute arguments parented by the given node.
        /// </summary>
        IEnumerable<SyntaxNode> GetAttributeArgumentNodes(SyntaxNode parent);

        /// <summary>
        /// Retrieves the Inherits nodes (i.e. VB Inherits statements) parented 
        /// or owned by the given node.
        /// </summary>
        IEnumerable<SyntaxNode> GetInheritsNodes(SyntaxNode parent);

        /// <summary>
        /// Retrieves the Implements nodes (i.e. VB Implements statements) parented 
        /// or owned by the given node.
        /// </summary>
        IEnumerable<SyntaxNode> GetImplementsNodes(SyntaxNode parent);

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
        IEnumerable<SyntaxNode> GetMemberNodes(SyntaxNode container, bool includeSelf, bool recursive, bool logicalFields, bool onlySupportedNodes);

        IEnumerable<SyntaxNode> GetLogicalSupportedMemberNodes(SyntaxNode container);

        SyntaxNodeKey GetNodeKey(SyntaxNode node);
        SyntaxNodeKey TryGetNodeKey(SyntaxNode node);
        SyntaxNode LookupNode(SyntaxNodeKey nodeKey, SyntaxTree syntaxTree);
        bool TryLookupNode(SyntaxNodeKey nodeKey, SyntaxTree syntaxTree, [NotNullWhen(true)] out SyntaxNode? node);

        bool MatchesScope(SyntaxNode node, EnvDTE.vsCMElement scope);

        string Language { get; }
        string AssemblyAttributeString { get; }

        /// <summary>
        /// Do not use this method directly! Instead, go through <see cref="FileCodeModel.GetOrCreateCodeElement{T}(SyntaxNode)"/>
        /// </summary>
        EnvDTE.CodeElement CreateInternalCodeElement(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node);
        EnvDTE.CodeElement CreateExternalCodeElement(CodeModelState state, ProjectId projectId, ISymbol symbol);
        EnvDTE.CodeElement CreateUnknownCodeElement(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node);
        EnvDTE.CodeElement CreateUnknownRootNamespaceCodeElement(CodeModelState state, FileCodeModel fileCodeModel);

        EnvDTE.CodeElement CreateCodeType(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol);

        /// <summary>
        /// Used by RootCodeModel.CreateCodeTypeRef to create an EnvDTE.CodeTypeRef.
        /// </summary>
        EnvDTE.CodeTypeRef CreateCodeTypeRef(CodeModelState state, ProjectId projectId, object type);

        EnvDTE.vsCMTypeRef GetTypeKindForCodeTypeRef(ITypeSymbol typeSymbol);
        string GetAsFullNameForCodeTypeRef(ITypeSymbol typeSymbol);
        string GetAsStringForCodeTypeRef(ITypeSymbol typeSymbol);

        bool IsParameterNode(SyntaxNode node);
        bool IsAttributeNode(SyntaxNode node);
        bool IsAttributeArgumentNode(SyntaxNode node);
        bool IsOptionNode(SyntaxNode node);
        bool IsImportNode(SyntaxNode node);

        ISymbol? ResolveSymbol(Microsoft.CodeAnalysis.Workspace workspace, ProjectId projectId, SymbolKey symbolId);

        string GetUnescapedName(string name);

        /// <summary>
        /// Retrieves the value to be returned from the EnvDTE.CodeElement.Name property.
        /// </summary>
        string GetName(SyntaxNode node);
        SyntaxNode GetNodeWithName(SyntaxNode node);
        SyntaxNode SetName(SyntaxNode node, string name);

        /// <summary>
        /// Retrieves the value to be returned from the EnvDTE.CodeElement.FullName property.
        /// </summary>
        string GetFullName(SyntaxNode node, SemanticModel semanticModel);

        /// <summary>
        /// Given a name, attempts to convert it to a fully qualified name.
        /// </summary>
        string GetFullyQualifiedName(string name, int position, SemanticModel semanticModel);

        void Rename(ISymbol symbol, string newName, Workspace workspace, ProjectCodeModelFactory projectCodeModelFactory);

        /// <summary>
        /// Returns true if the given <paramref name="symbol"/> can be used to create an external code element; otherwise, false.
        /// </summary>
        bool IsValidExternalSymbol(ISymbol symbol);

        /// <summary>
        /// Returns the value to be returned from <see cref="EnvDTE.CodeElement.Name"/> for external code elements.
        /// </summary>
        string GetExternalSymbolName(ISymbol symbol);

        /// <summary>
        /// Retrieves the value to be returned from <see cref="EnvDTE.CodeElement.FullName"/> for external code elements.
        /// </summary>
        string GetExternalSymbolFullName(ISymbol symbol);

#nullable disable
        SyntaxNode GetNodeWithModifiers(SyntaxNode node);
        SyntaxNode GetNodeWithType(SyntaxNode node);
#nullable restore
        SyntaxNode GetNodeWithInitializer(SyntaxNode node);

        EnvDTE.vsCMAccess GetAccess(ISymbol symbol);
        EnvDTE.vsCMAccess GetAccess(SyntaxNode node);
        SyntaxNode SetAccess(SyntaxNode node, EnvDTE.vsCMAccess access);
        EnvDTE.vsCMElement GetElementKind(SyntaxNode node);

        bool IsExpressionBodiedProperty(SyntaxNode node);
        bool IsAccessorNode(SyntaxNode node);
        MethodKind GetAccessorKind(SyntaxNode node);

        bool TryGetAccessorNode(SyntaxNode parentNode, MethodKind kind, [NotNullWhen(true)] out SyntaxNode? accessorNode);
        bool TryGetAutoPropertyExpressionBody(SyntaxNode parentNode, [NotNullWhen(true)] out SyntaxNode? expressionBody);
        bool TryGetParameterNode(SyntaxNode parentNode, string name, [NotNullWhen(true)] out SyntaxNode? parameterNode);
        bool TryGetImportNode(SyntaxNode parentNode, string dottedName, [NotNullWhen(true)] out SyntaxNode? importNode);
        bool TryGetOptionNode(SyntaxNode parentNode, string name, int ordinal, [NotNullWhen(true)] out SyntaxNode? optionNode);
        bool TryGetInheritsNode(SyntaxNode parentNode, string name, int ordinal, [NotNullWhen(true)] out SyntaxNode? inheritsNode);
        bool TryGetImplementsNode(SyntaxNode parentNode, string name, int ordinal, [NotNullWhen(true)] out SyntaxNode? implementsNode);
        bool TryGetAttributeNode(SyntaxNode parentNode, string name, int ordinal, [NotNullWhen(true)] out SyntaxNode? attributeNode);
        bool TryGetAttributeArgumentNode(SyntaxNode attributeNode, int index, [NotNullWhen(true)] out SyntaxNode? attributeArgumentNode);

        void GetOptionNameAndOrdinal(SyntaxNode parentNode, SyntaxNode optionNode, out string name, out int ordinal);
        void GetInheritsNamespaceAndOrdinal(SyntaxNode parentNode, SyntaxNode inheritsNode, out string namespaceName, out int ordinal);
        void GetImplementsNamespaceAndOrdinal(SyntaxNode parentNode, SyntaxNode implementsNode, out string namespaceName, out int ordinal);

        void GetAttributeArgumentParentAndIndex(SyntaxNode attributeArgumentNode, out SyntaxNode attributeNode, out int index);

        void GetAttributeNameAndOrdinal(SyntaxNode parentNode, SyntaxNode attributeNode, out string name, out int ordinal);
        SyntaxNode GetAttributeTargetNode(SyntaxNode attributeNode);
        string GetAttributeTarget(SyntaxNode attributeNode);
        string GetAttributeValue(SyntaxNode attributeNode);
        SyntaxNode SetAttributeTarget(SyntaxNode attributeNode, string value);
        SyntaxNode SetAttributeValue(SyntaxNode attributeNode, string value);

        /// <summary>
        /// Given a node, finds the related node that holds on to the attribute information.
        /// Generally, this will be an ancestor node. For example, given a C# VariableDeclarator,
        /// looks up the syntax tree to find the FieldDeclaration.
        /// </summary>
        SyntaxNode GetNodeWithAttributes(SyntaxNode node);

        /// <summary>
        /// Given node for an attribute, returns a node that can represent the parent.
        /// For example, an attribute on a C# field cannot use the FieldDeclaration (as it is
        /// not keyed) but instead must use one of the FieldDeclaration's VariableDeclarators.
        /// </summary>
        SyntaxNode GetEffectiveParentForAttribute(SyntaxNode node);

        SyntaxNode CreateAttributeNode(string name, string value, string? target = null);
        SyntaxNode CreateAttributeArgumentNode(string name, string value);
        SyntaxNode CreateImportNode(string name, string? alias = null);
        SyntaxNode CreateParameterNode(string name, string type);

        string GetAttributeArgumentValue(SyntaxNode attributeArgumentNode);

        string GetImportAlias(SyntaxNode node);
        string GetImportNamespaceOrType(SyntaxNode node);
        string GetParameterName(SyntaxNode node);
        string GetParameterFullName(SyntaxNode node);
        EnvDTE80.vsCMParameterKind GetParameterKind(SyntaxNode node);
        SyntaxNode SetParameterKind(SyntaxNode node, EnvDTE80.vsCMParameterKind kind);
        IEnumerable<SyntaxNode> GetParameterNodes(SyntaxNode parent);
        EnvDTE80.vsCMParameterKind UpdateParameterKind(EnvDTE80.vsCMParameterKind parameterKind, PARAMETER_PASSING_MODE passingMode);

        EnvDTE.vsCMFunction ValidateFunctionKind(SyntaxNode containerNode, EnvDTE.vsCMFunction kind, string name);

        bool SupportsEventThrower { get; }

        bool GetCanOverride(SyntaxNode memberNode);
        SyntaxNode SetCanOverride(SyntaxNode memberNode, bool value);

        EnvDTE80.vsCMClassKind GetClassKind(SyntaxNode typeNode, INamedTypeSymbol typeSymbol);
        SyntaxNode SetClassKind(SyntaxNode typeNode, EnvDTE80.vsCMClassKind kind);

        string GetComment(SyntaxNode node);
        SyntaxNode SetComment(SyntaxNode node, string value);

        EnvDTE80.vsCMConstKind GetConstKind(SyntaxNode variableNode);
        SyntaxNode SetConstKind(SyntaxNode variableNode, EnvDTE80.vsCMConstKind kind);

        EnvDTE80.vsCMDataTypeKind GetDataTypeKind(SyntaxNode typeNode, INamedTypeSymbol symbol);
        SyntaxNode SetDataTypeKind(SyntaxNode typeNode, EnvDTE80.vsCMDataTypeKind kind);

        string GetDocComment(SyntaxNode node);
        SyntaxNode SetDocComment(SyntaxNode node, string value);

        EnvDTE.vsCMFunction GetFunctionKind(IMethodSymbol symbol);

        EnvDTE80.vsCMInheritanceKind GetInheritanceKind(SyntaxNode typeNode, INamedTypeSymbol typeSymbol);
        SyntaxNode SetInheritanceKind(SyntaxNode node, EnvDTE80.vsCMInheritanceKind kind);

        bool GetIsAbstract(SyntaxNode memberNode, ISymbol symbol);
        SyntaxNode SetIsAbstract(SyntaxNode memberNode, bool value);

        bool GetIsConstant(SyntaxNode memberNode);
        SyntaxNode SetIsConstant(SyntaxNode memberNode, bool value);

        bool GetIsDefault(SyntaxNode propertyNode);
        SyntaxNode SetIsDefault(SyntaxNode propertyNode, bool value);

        bool GetIsGeneric(SyntaxNode memberNode);

        bool GetIsPropertyStyleEvent(SyntaxNode eventNode);

        bool GetIsShared(SyntaxNode memberNode, ISymbol symbol);
        SyntaxNode SetIsShared(SyntaxNode memberNode, bool value);

        bool GetMustImplement(SyntaxNode memberNode);
        SyntaxNode SetMustImplement(SyntaxNode memberNode, bool value);

        EnvDTE80.vsCMOverrideKind GetOverrideKind(SyntaxNode memberNode);
        SyntaxNode SetOverrideKind(SyntaxNode memberNode, EnvDTE80.vsCMOverrideKind kind);

        EnvDTE80.vsCMPropertyKind GetReadWrite(SyntaxNode memberNode);

        SyntaxNode SetType(SyntaxNode node, ITypeSymbol? typeSymbol);

        Document Delete(Document document, SyntaxNode node);

        string GetMethodXml(SyntaxNode node, SemanticModel semanticModel);

        string? GetInitExpression(SyntaxNode node);
        SyntaxNode AddInitExpression(SyntaxNode node, string value);

        CodeGenerationDestination GetDestination(SyntaxNode containerNode);

        /// <summary>
        /// Retrieves the Accessibility for an EnvDTE.vsCMAccess. If the specified value is
        /// EnvDTE.vsCMAccess.vsCMAccessDefault, then the SymbolKind and CodeGenerationDestination hints
        /// will be used to retrieve the correct Accessibility for the current language.
        /// </summary>
        Accessibility GetAccessibility(EnvDTE.vsCMAccess access, SymbolKind targetSymbolKind, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified);
        bool GetWithEvents(EnvDTE.vsCMAccess access);

        /// <summary>
        /// Given an "type" argument received from a CodeModel client, converts it to an ITypeSymbol. Note that
        /// this parameter is a VARIANT and could be an EnvDTE.vsCMTypeRef, a string representing a fully-qualified
        /// type name, or an EnvDTE.CodeTypeRef.
        /// </summary>
        ITypeSymbol GetTypeSymbol(object type, SemanticModel semanticModel, int position);

        ITypeSymbol? GetTypeSymbolFromFullName(string fullName, Compilation compilation);

        SyntaxNode CreateReturnDefaultValueStatement(ITypeSymbol type);

        int PositionVariantToAttributeInsertionIndex(object position, SyntaxNode containerNode, FileCodeModel fileCodeModel);
        int PositionVariantToMemberInsertionIndex(object position, SyntaxNode containerNode, FileCodeModel fileCodeModel);
        int PositionVariantToAttributeArgumentInsertionIndex(object position, SyntaxNode containerNode, FileCodeModel fileCodeModel);
        int PositionVariantToImportInsertionIndex(object position, SyntaxNode containerNode, FileCodeModel fileCodeModel);
        int PositionVariantToParameterInsertionIndex(object position, SyntaxNode containerNode, FileCodeModel fileCodeModel);

        SyntaxNode InsertAttribute(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode attributeNode,
            CancellationToken cancellationToken,
            out Document newDocument);

        SyntaxNode InsertAttributeArgument(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode attributeArgumentNode,
            CancellationToken cancellationToken,
            out Document newDocument);

        SyntaxNode InsertImport(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode importNode,
            CancellationToken cancellationToken,
            out Document newDocument);

        SyntaxNode InsertMember(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode newMemberNode,
            CancellationToken cancellationToken,
            out Document newDocument);

        SyntaxNode InsertParameter(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode parameterNode,
            CancellationToken cancellationToken,
            out Document newDocument);

        Document UpdateNode(
            Document document,
            SyntaxNode node,
            SyntaxNode newNode,
            CancellationToken cancellationToken);

        Queue<CodeModelEvent> CollectCodeModelEvents(SyntaxTree oldTree, SyntaxTree newTree);

        bool IsNamespace(SyntaxNode node);
        bool IsType(SyntaxNode node);

        IList<string> GetHandledEventNames(SyntaxNode method, SemanticModel semanticModel);
        bool HandlesEvent(string eventName, SyntaxNode method, SemanticModel semanticModel);
        Document AddHandlesClause(Document document, string eventName, SyntaxNode method, CancellationToken cancellationToken);
        Document RemoveHandlesClause(Document document, string eventName, SyntaxNode method, CancellationToken cancellationToken);

        string[] GetFunctionExtenderNames();
        object GetFunctionExtender(string name, SyntaxNode node, ISymbol symbol);
        string[] GetPropertyExtenderNames();
        object GetPropertyExtender(string name, SyntaxNode node, ISymbol symbol);
        string[] GetExternalTypeExtenderNames();
        object GetExternalTypeExtender(string name, string externalLocation);
        string[] GetTypeExtenderNames();
        object GetTypeExtender(string name, AbstractCodeType codeType);

        bool IsValidBaseType(SyntaxNode node, ITypeSymbol typeSymbol);
        SyntaxNode AddBase(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel, int? position);
        SyntaxNode RemoveBase(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel);

        bool IsValidInterfaceType(SyntaxNode node, ITypeSymbol typeSymbol);
        SyntaxNode AddImplementedInterface(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel, int? position);
        SyntaxNode RemoveImplementedInterface(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel);

        string GetPrototype(SyntaxNode node, ISymbol symbol, PrototypeFlags flags);

        void AttachFormatTrackingToBuffer(ITextBuffer buffer);
        void DetachFormatTrackingToBuffer(ITextBuffer buffer);
        void EnsureBufferFormatted(ITextBuffer buffer);
    }
}
