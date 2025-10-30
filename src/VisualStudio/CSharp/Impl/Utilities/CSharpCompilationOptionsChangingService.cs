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

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Utilities;

[ExportLanguageService(typeof(ICompilationOptionsChangingService), LanguageNames.CSharp), Shared]
internal sealed class CSharpCompilationOptionsChangingService : ICompilationOptionsChangingService
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

        // Currently, only changes to AllowUnsafe and Nullable of compilation options are supported.
        return oldCSharpOptions.WithAllowUnsafe(newCSharpOptions.AllowUnsafe).WithNullableContextOptions(newCSharpOptions.NullableContextOptions) == newOptions;
    }

    public void Apply(CompilationOptions oldOptions, CompilationOptions newOptions, ProjectPropertyStorage storage)
    {
        var oldCSharpOptions = (CSharpCompilationOptions)oldOptions;
        var newCSharpOptions = (CSharpCompilationOptions)newOptions;

        if (newCSharpOptions.AllowUnsafe != oldCSharpOptions.AllowUnsafe)
        {
            storage.SetProperty("AllowUnsafeBlocks", nameof(ProjectConfigurationProperties3.AllowUnsafeBlocks),
                newCSharpOptions.AllowUnsafe);
        }

        if (newCSharpOptions.NullableContextOptions != oldCSharpOptions.NullableContextOptions)
        {
            var projectSetting = newCSharpOptions.NullableContextOptions switch
            {
                NullableContextOptions.Enable => "enable",
                NullableContextOptions.Warnings => "warnings",
                NullableContextOptions.Annotations => "annotations",
                _ => "disable",
            };
            storage.SetProperty("Nullable", "Nullable", projectSetting);
        }
    }
}
