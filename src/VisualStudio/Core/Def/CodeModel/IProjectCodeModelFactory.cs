// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

internal interface IProjectCodeModelFactory
{
    IProjectCodeModel CreateProjectCodeModel(ProjectId id, ICodeModelInstanceFactory codeModelInstanceFactory);
    EnvDTE.FileCodeModel GetOrCreateFileCodeModel(ProjectId id, string filePath);
    EnvDTE.FileCodeModel CreateFileCodeModel(SourceGeneratedDocument sourceGeneratedDocument);
}
