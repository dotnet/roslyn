// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Enum values for completion item kinds.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionItemKind">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal enum CompletionItemKind
    {
        /// <summary>
        /// Value to use when no kind was provided.
        /// </summary>
        None = 0,

        // LSP Spec v3.16:

        /// <summary>
        /// Text.
        /// </summary>
        Text = 1,

        /// <summary>
        /// Method.
        /// </summary>
        Method = 2,

        /// <summary>
        /// Function.
        /// </summary>
        Function = 3,

        /// <summary>
        /// Constructor.
        /// </summary>
        Constructor = 4,

        /// <summary>
        /// Field.
        /// </summary>
        Field = 5,

        /// <summary>
        /// Variable.
        /// </summary>
        Variable = 6,

        /// <summary>
        /// Class.
        /// </summary>
        Class = 7,

        /// <summary>
        /// Interface.
        /// </summary>
        Interface = 8,

        /// <summary>
        /// Module.
        /// </summary>
        Module = 9,

        /// <summary>
        /// Property.
        /// </summary>
        Property = 10,

        /// <summary>
        /// Unit.
        /// </summary>
        Unit = 11,

        /// <summary>
        /// Value.
        /// </summary>
        Value = 12,

        /// <summary>
        /// Enum.
        /// </summary>
        Enum = 13,

        /// <summary>
        /// Keyword.
        /// </summary>
        Keyword = 14,

        /// <summary>
        /// Snippet.
        /// </summary>
        Snippet = 15,

        /// <summary>
        /// Color.
        /// </summary>
        Color = 16,

        /// <summary>
        /// File.
        /// </summary>
        File = 17,

        /// <summary>
        /// Reference.
        /// </summary>
        Reference = 18,

        /// <summary>
        /// Folder.
        /// </summary>
        Folder = 19,

        /// <summary>
        /// EnumMember.
        /// </summary>
        EnumMember = 20,

        /// <summary>
        /// Constant.
        /// </summary>
        Constant = 21,

        /// <summary>
        /// Struct.
        /// </summary>
        Struct = 22,

        /// <summary>
        /// Event.
        /// </summary>
        Event = 23,

        /// <summary>
        /// Operator.
        /// </summary>
        Operator = 24,

        /// <summary>
        /// TypeParameter.
        /// </summary>
        TypeParameter = 25,

        // Kinds custom to VS, starting with index 118115 to avoid collisions with other clients's custom kinds.

        /// <summary>
        /// Macro.
        /// </summary>
        Macro = 118115 + 0,

        /// <summary>
        /// Namespace.
        /// </summary>
        Namespace = 118115 + 1,

        /// <summary>
        /// Template.
        /// </summary>
        Template = 118115 + 2,

        /// <summary>
        /// TypeDefinition.
        /// </summary>
        TypeDefinition = 118115 + 3,

        /// <summary>
        /// Union.
        /// </summary>
        Union = 118115 + 4,

        /// <summary>
        /// Delegate.
        /// </summary>
        Delegate = 118115 + 5,

        /// <summary>
        /// TagHelper.
        /// </summary>
        TagHelper = 118115 + 6,

        /// <summary>
        /// ExtensionMethod.
        /// </summary>
        ExtensionMethod = 118115 + 7,

        /// <summary>
        /// Element.
        /// </summary>
        Element = 118115 + 8,

        /// <summary>
        /// LocalResource.
        /// </summary>
        LocalResource = 118115 + 9,

        /// <summary>
        /// SystemResource.
        /// </summary>
        SystemResource = 118115 + 10,

        /// <summary>
        /// CloseElement.
        /// </summary>
        CloseElement = 118115 + 11,
    }
}
