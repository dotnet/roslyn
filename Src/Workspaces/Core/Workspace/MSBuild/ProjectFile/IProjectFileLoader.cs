// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal interface IProjectFileLoader : ILanguageService
    {
        string Language { get; }
        bool IsProjectTypeGuid(Guid guid);
        bool IsProjectFileExtension(string fileExtension);
        Task<IProjectFile> LoadProjectFileAsync(string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken);
    }
}
