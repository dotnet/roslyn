// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [ExcludeFromCodeCoverage]
    [Guid(Guids.CSharpEditorFactoryIdString)]
    internal class CSharpEditorFactory : AbstractEditorFactory
    {
        public CSharpEditorFactory(IComponentModel componentModel)
            : base(componentModel)
        {
        }

        protected override string ContentTypeName => ContentTypeNames.CSharpContentType;
        protected override string LanguageName => LanguageNames.CSharp;

        protected override Solution GetSolutionWithCorrectParseOptionsForProject(ProjectId projectId, IVsHierarchy hierarchy, Solution solution)
        {
            var project = solution.GetRequiredProject(projectId);

            if (project.ParseOptions is CSharpParseOptions parseOptions &&
                hierarchy is IVsBuildPropertyStorage propertyStorage &&
                ErrorHandler.Succeeded(propertyStorage.GetPropertyValue("LangVersion", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out var langVersionString)) &&
                LanguageVersionFacts.TryParse(langVersionString, out var langVersion))
            {
                return solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(langVersion));
            }

            return solution;
        }
    }
}
