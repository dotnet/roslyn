// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal interface IProjectShimFactory
    {
        IProjectShim CreateProjectShim(string languageName, string projectName, CommandLineArguments commandLineArguments);
    }
}
