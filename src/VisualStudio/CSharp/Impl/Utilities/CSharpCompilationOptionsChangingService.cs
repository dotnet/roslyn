// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using VSLangProj80;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Utilities
{
    [ExportLanguageService(typeof(ICompilationOptionsChangingService), LanguageNames.CSharp), Shared]
    internal class CSharpCompilationOptionsChangingService : ICompilationOptionsChangingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCompilationOptionsChangingService()
        {
        }

        public bool CanApplyChange(CompilationOptions oldOptions, CompilationOptions newOptions)
        {
            var oldCSharpOptions = (CSharpCompilationOptions)oldOptions;
            var newCSharpOptions = (CSharpCompilationOptions)newOptions;

            // Currently, only changes to AllowUnsafe of compilation options are supported.
            return oldCSharpOptions.WithAllowUnsafe(newCSharpOptions.AllowUnsafe) == newOptions;
        }

        public void Apply(CompilationOptions options, ProjectPropertyStorage storage)
        {
            var csharpOptions = (CSharpCompilationOptions)options;

            storage.SetProperty("AllowUnsafeBlocks", nameof(ProjectConfigurationProperties3.AllowUnsafeBlocks),
                csharpOptions.AllowUnsafe);
        }
    }
}
