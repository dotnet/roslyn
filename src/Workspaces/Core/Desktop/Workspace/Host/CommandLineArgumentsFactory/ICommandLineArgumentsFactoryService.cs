// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface ICommandLineArgumentsFactoryService : ILanguageService
    {
        CommandLineArguments CreateCommandLineArguments(IEnumerable<string> arguments, string baseDirectory, bool isInteractive);
    }
}
