// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal interface IProjectFileLoader : ILanguageService
    {
        string Language { get; }
        IRemoteTask<IProjectFile> LoadProjectFileAsync(string path, Dictionary<string, string> globalProperties);
    }
}
