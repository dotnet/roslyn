// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Value representing the kind of a code action.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionKind">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter<CodeActionKind>))]
    [TypeConverter(typeof(StringEnumConverter<CodeActionKind>.TypeConverter))]
    internal readonly record struct CodeActionKind(string Value) : IStringEnum
    {
        /// <summary>
        /// Empty kind.
        /// </summary>
        public static readonly CodeActionKind Empty = new(string.Empty);

        /// <summary>
        /// Code action is a refactor.
        /// </summary>
        public static readonly CodeActionKind QuickFix = new("quickfix");

        /// <summary>
        /// Base kind for refactoring actions.
        /// </summary>
        public static readonly CodeActionKind Refactor = new("refactor");

        /// <summary>
        /// Base kind for refactoring extraction actions, like extracting methods, functions,
        /// variables, etc.
        /// </summary>
        public static readonly CodeActionKind RefactorExtract = new("refactor.extract");

        /// <summary>
        /// Base kind for refactoring inline actions, like inlining functions, variables,
        /// constants, etc.
        /// </summary>
        public static readonly CodeActionKind RefactorInline = new("refactor.inline");

        /// <summary>
        /// Base kind for refactoring rewrite actions, like adding or removing a parameter,
        /// making a method static, etc.
        /// </summary>
        public static readonly CodeActionKind RefactorRewrite = new("refactor.rewrite");

        /// <summary>
        /// Base kind for source actions, which apply to the entire file.
        /// </summary>
        public static readonly CodeActionKind Source = new("source");

        /// <summary>
        /// Base kind for an organize imports source action.
        /// </summary>
        public static readonly CodeActionKind SourceOrganizeImports = new("source.organizeImports");

        /// <summary>
        /// Base kind for a fix all source action, which automatically fixes errors that have a clear
        /// fix that do not require user input.
        /// <para>
        /// They should not suppress errors or perform unsafe fixes such as generating new
        /// types or classes.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Since LSP 3.17
        /// </remarks>
        public static readonly CodeActionKind SourceFixAll = new("source.fixAll");
    }
}
