// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

// navigation methods from https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#languageFeatures
partial class Methods
{
    // NOTE: these are sorted in the order used by the spec

    /// <summary>
    /// Method name for 'textDocument/declaration'.
    /// <para>
    /// The go to declaration request is sent from the client to the server to resolve the declaration location of a symbol at a given text document position.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_declaration">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.14</remarks>
    public const string TextDocumentDeclarationName = "textDocument/declaration";

    /// <summary>
    /// Strongly typed message object for 'textDocument/declaration'.
    /// <para>
    /// <see cref="LocationLink"/> may only be returned if the client opts in via <see cref="DeclarationClientCapabilities.LinkSupport"/>
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.14</remarks>
    public static readonly LspRequest<DeclarationParams, SumType<Location, Location[], LocationLink[]>?> TextDocumentDeclaration = new(TextDocumentDeclarationName);

    /// <summary>
    /// Method name for 'textDocument/definition'.
    /// <para>
    /// The go to definition request is sent from the client to the server to resolve the definition location of a symbol at a given text document position.
    /// </para>
    /// <para>
    /// <see cref="LocationLink"/> may only be returned if the client opts in via <see cref="DefinitionClientCapabilities.LinkSupport"/>
    /// </para>
    /// </summary>
    public const string TextDocumentDefinitionName = "textDocument/definition";

    /// <summary>
    /// Strongly typed message object for 'textDocument/definition'.
    /// <para>
    /// <see cref="LocationLink"/> may only be returned if the client opts in via <see cref="DefinitionClientCapabilities.LinkSupport"/>
    /// </para>
    /// </summary>
    public static readonly LspRequest<DefinitionParams, SumType<Location, Location[], LocationLink[]>?> TextDocumentDefinition = new(TextDocumentDefinitionName);

    /// <summary>
    /// Method name for 'textDocument/typeDefinition'.
    /// <para>
    /// The go to type definition request is sent from the client to the server to resolve the type definition location of a symbol at a given text document position.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_typeDefinition">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentTypeDefinitionName = "textDocument/typeDefinition";

    /// <summary>
    /// Strongly typed message object for 'textDocument/typeDefinition'.
    /// <para>
    /// <see cref="LocationLink"/> may only be returned if the client opts in via <see cref="TypeDefinitionClientCapabilities.LinkSupport"/>
    /// </para>
    /// </summary>
    public static readonly LspRequest<TypeDefinitionParams, SumType<Location, Location[], LocationLink[]>?> TextDocumentTypeDefinition = new(TextDocumentTypeDefinitionName);

    /// <summary>
    /// Method name for 'textDocument/implementation'.
    /// <para>
    /// The go to implementation request is sent from the client to the server to resolve the implementation location of a symbol at a given text document position.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_implementation">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentImplementationName = "textDocument/implementation";

    /// <summary>
    /// Strongly typed message object for 'textDocument/implementation'.
    /// <para>
    /// <see cref="LocationLink"/> may only be returned if the client opts in via <see cref="TypeDefinitionClientCapabilities.LinkSupport"/>
    /// </para>
    /// </summary>
    public static readonly LspRequest<ImplementationParams, SumType<Location, Location[], LocationLink[]>?> TextDocumentImplementation = new(TextDocumentImplementationName);

    /// <summary>
    /// Method name for 'textDocument/references'.
    /// <para>
    /// The references request is sent from the client to the server to resolve project-wide references for the symbol denoted by the given text document position.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_references">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentReferencesName = "textDocument/references";

    /// <summary>
    /// Strongly typed message object for 'textDocument/references'.
    /// </summary>
    public static readonly LspRequest<ReferenceParams, Location[]?> TextDocumentReferences = new(TextDocumentReferencesName);

    /// <summary>
    /// Method name for 'textDocument/prepareCallHierarchy'.
    /// <para>
    /// The call hierarchy request is sent from the client to the server to return a call hierarchy for the language element of given text document positions. The call hierarchy requests are executed in two steps:
    /// <list type="bullet">
    /// <item>first a call hierarchy item is resolved for the given text document position</item>
    /// <item>for a call hierarchy item the incoming or outgoing call hierarchy items are resolved.</item>
    /// </list>
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_prepareCallHierarchy">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string PrepareCallHierarchyName = "textDocument/prepareCallHierarchy";

    /// <summary>
    /// Strongly typed message object for 'textDocument/prepareCallHierarchy'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<CallHierarchyPrepareParams, CallHierarchyItem[]?> PrepareCallHierarchy = new(PrepareCallHierarchyName);

    /// <summary>
    /// Method name for 'callHierarchy/incomingCalls'.
    /// <para>
    /// The request is sent from the client to the server to resolve incoming calls for a given call hierarchy item.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#callHierarchy_incomingCalls">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string CallHierarchyIncomingCallsName = "callHierarchy/incomingCalls";

    /// <summary>
    /// Strongly typed message object for 'callHierarchy/incomingCalls'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<CallHierarchyIncomingCallsParams, CallHierarchyIncomingCall[]?> CallHierarchyIncomingCalls = new(CallHierarchyIncomingCallsName);

    /// <summary>
    /// Method name for 'callHierarchy/outgoingCalls'.
    /// <para>
    /// The request is sent from the client to the server to resolve outgoing calls for a given call hierarchy item.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#callHierarchy_outgoingCalls">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string CallHierarchyOutgoingCallsName = "callHierarchy/outgoingCalls";

    /// <summary>
    /// Strongly typed message object for 'callHierarchy/outgoingCalls'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<CallHierarchyOutgoingCallsParams, CallHierarchyOutgoingCall[]?> CallHierarchyOutgoingCalls = new(CallHierarchyOutgoingCallsName);

    /// <summary>
    /// Method name for 'textDocument/prepareTypeHierarchy'.
    /// <para>
    /// The type hierarchy request is sent from the client to the server to return a type hierarchy for the language element of given text
    /// document positions. Will return null if the server couldn't infer a valid type from the position.
    /// <para>
    /// </para>
    /// The type hierarchy requests are executed in two steps:
    /// <list type="bullet">
    /// <item>first a type hierarchy item is prepared for the given text document position.</item>
    /// <item>for a type hierarchy item the supertype or subtype type hierarchy items are resolved.</item>
    /// </list>
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_prepareTypeHierarchy">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string PrepareTypeHierarchyName = "textDocument/prepareTypeHierarchy";

    /// <summary>
    /// Strongly typed message object for 'textDocument/prepareTypeHierarchy'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspRequest<TypeHierarchyPrepareParams, TypeHierarchyItem[]?> PrepareTypeHierarchy = new(PrepareTypeHierarchyName);

    /// <summary>
    /// Method name for 'typeHierarchy/supertypes'.
    /// <para>
    /// The request is sent from the client to the server to resolve the supertypes for a given type hierarchy item.
    /// Will return null if the server couldn't infer a valid type from item in the params.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#typeHierarchy_supertypes">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string TypeHierarchySupertypesName = "typeHierarchy/supertypes";

    /// <summary>
    /// Strongly typed message object for 'typeHierarchy/supertypes'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspRequest<TypeHierarchySupertypesParams, TypeHierarchyItem[]?> TypeHierarchySupertypes = new(TypeHierarchySupertypesName);

    /// <summary>
    /// Method name for 'typeHierarchy/subtypes'.
    /// <para>
    /// The request is sent from the client to the server to resolve outgoing calls for a given call hierarchy item.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#typeHierarchy_subtypes">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TypeHierarchySubtypesName = "typeHierarchy/subtypes";

    /// <summary>
    /// Strongly typed message object for 'typeHierarchy/subtypes'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspRequest<TypeHierarchySubtypesParams, TypeHierarchyItem[]?> TypeHierarchySubtypes = new(TypeHierarchySubtypesName);
}
