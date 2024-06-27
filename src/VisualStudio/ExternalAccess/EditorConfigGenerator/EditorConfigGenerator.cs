﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.EditorConfigGenerator.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfigGenerator;

[Export(typeof(IEditorConfigGenerator)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorConfigGenerator(IGlobalOptionService globalOptions, EditorConfigOptionsEnumerator enumerator) : IEditorConfigGenerator
{
    public string? Generate(string language)
        => EditorConfigFileGenerator.Generate(enumerator.GetOptions(language), globalOptions, language);
}
