// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    internal static class NoCompilationContentTypeDefinitions
    {
        /// <summary>
        /// Definition of the primary C# content type.
        /// </summary>
        [System.ComponentModel.Composition.Export]
        [Name(NoCompilationConstants.LanguageName)]
        [BaseDefinition(ContentTypeNames.RoslynContentType)]
        public static readonly ContentTypeDefinition NoCompilationContentTypeDefinition;
    }
}
