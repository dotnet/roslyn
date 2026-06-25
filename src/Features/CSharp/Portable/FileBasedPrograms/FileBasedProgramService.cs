// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

[Export(typeof(IFileBasedProgramService)), Shared]
[ExportLanguageService(typeof(IFileBasedProgramService), LanguageNames.CSharp)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FileBasedProgramService() : IFileBasedProgramService
{
    public object LanguageService => Microsoft.DotNet.FileBasedPrograms.LanguageService.Instance;
}
