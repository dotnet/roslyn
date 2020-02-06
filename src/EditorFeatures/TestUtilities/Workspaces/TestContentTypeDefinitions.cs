// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    internal static class TestContentTypeDefinitions
    {
        /// <summary>
        /// Definition of the primary C# content type.
        /// </summary>
        [System.ComponentModel.Composition.Export]
        [Name(NoCompilationConstants.LanguageName)]
        [BaseDefinition(ContentTypeNames.RoslynContentType)]
        public static readonly ContentTypeDefinition NoCompilationContentTypeDefinition;

        /// <summary>
        /// Definition of HTML content type.
        /// </summary>
        [System.ComponentModel.Composition.Export]
        [Name("HTML")]
        [BaseDefinition("text")]
        public static readonly ContentTypeDefinition Html;

        /// <summary>
        /// Definition of HTML content type.
        /// </summary>
        [System.ComponentModel.Composition.Export]
        [Name("Razor")]
        [BaseDefinition("text")]
        public static readonly ContentTypeDefinition Razor;
    }
}
