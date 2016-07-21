// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    internal partial class AbstractLegacyProject : ICompilerOptionsHostObject
    {
        int ICompilerOptionsHostObject.SetCompilerOptions(string compilerOptions, out bool supported)
        {
            CommandLineArguments commandLineArguments;
            if (TryGetNewCommandLineArguments(compilerOptions, out commandLineArguments))
            {
                base.SetCommandLineArguments(commandLineArguments);
            }

            supported = true;
            return VSConstants.S_OK;
        }

        protected abstract CommandLineArguments ParseCommandLineArguments(IEnumerable<string> splitArguments);

        private bool TryGetNewCommandLineArguments(string compilerOptions, out CommandLineArguments commandLineArguments)
        {
            if (!string.Equals(_lastParsedCompilerOptions, compilerOptions, StringComparison.OrdinalIgnoreCase))
            {
                var splitArguments = CommandLineParser.SplitCommandLineIntoArguments(compilerOptions, removeHashComments: false);
                commandLineArguments = ParseCommandLineArguments(splitArguments);
                _lastParsedCompilerOptions = compilerOptions;
                return true;
            }

            commandLineArguments = null;
            return false;
        }
    }
}
