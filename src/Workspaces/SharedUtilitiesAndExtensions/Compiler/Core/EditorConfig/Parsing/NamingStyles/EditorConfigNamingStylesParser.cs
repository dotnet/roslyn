// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles
{
    internal static class EditorConfigNamingStylesParser
    {
        /// <summary>
        /// Parses a string and returns all discovered naming style options and their locations
        /// </summary>
        /// <param name="editorConfigText">The text contents of the editorconfig file.</param>
        /// <param name="pathToEditorConfigFile">The full path to the editorconfig file on disk.</param>
        /// <returns>A type that represents all discovered naming style options in the given string.</returns>
        public static EditorConfigNamingStyles Parse(string editorConfigText, string? pathToEditorConfigFile = null)
            => Parse(SourceText.From(editorConfigText), pathToEditorConfigFile);

        /// <summary>
        /// Parses a <see cref="SourceText"/> and returns all discovered naming style options and their locations
        /// </summary>
        /// <param name="editorConfigText">The <see cref="SourceText"/> contents of the editorconfig file.</param>
        /// <param name="pathToEditorConfigFile">The full path to the editorconfig file on disk.</param>
        /// <returns>A type that represents all discovered naming style options in the given <see cref="SourceText"/>.</returns>
        public static EditorConfigNamingStyles Parse(SourceText editorConfigText, string? pathToEditorConfigFile = null)
            => EditorConfigParser.Parse<EditorConfigNamingStyles, NamingStyleOption, NamingStyleOptionAccumulator>(editorConfigText, pathToEditorConfigFile, new NamingStyleOptionAccumulator());
    }
}
