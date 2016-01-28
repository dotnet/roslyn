// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides common well-known project capabilities.
    /// </summary>
    internal static class ProjectCapability
    {
        public const string CSharp = ProjectCapabilities.CSharp;
        public const string VisualBasic = ProjectCapabilities.VB;
        public const string VisualBasicLanguageService = ProjectCapabilities.VB + " & " + ProjectCapabilities.LanguageService;
        public const string CSharpLanguageService = ProjectCapabilities.CSharp + " & " + ProjectCapabilities.LanguageService;
        public const string CSharpOrVisualBasic = ProjectCapabilities.CSharp + " | " + ProjectCapabilities.VB;
    }
}
