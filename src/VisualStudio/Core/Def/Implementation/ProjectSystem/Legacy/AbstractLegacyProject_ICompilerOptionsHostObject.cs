// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
{
    internal partial class AbstractLegacyProject : ICompilerOptionsHostObject
    {
        private string _lastParsedCompilerOptions;

        int ICompilerOptionsHostObject.SetCompilerOptions(string compilerOptions, out bool supported)
        {
            if (!string.Equals(_lastParsedCompilerOptions, compilerOptions, StringComparison.OrdinalIgnoreCase))
            {
                // Command line options have changed, so update options with new parsed CommandLineArguments.
                var splitArguments = CommandLineParser.SplitCommandLineIntoArguments(compilerOptions, removeHashComments: false);
                var commandLineArguments = ParseCommandLineArguments(splitArguments);
                SetArgumentsAndUpdateOptions(commandLineArguments);
                _lastParsedCompilerOptions = compilerOptions;
            }

            supported = true;
            return VSConstants.S_OK;
        }

        protected abstract CommandLineArguments ParseCommandLineArguments(IEnumerable<string> splitArguments);
    }
}
