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
            SetCompilerOptions(compilerOptions);
            supported = true;
            return VSConstants.S_OK;
        }

        protected abstract CommandLineArguments ParseCommandLineArguments(IEnumerable<string> splitArguments);

        private void SetCompilerOptions(string compilerOptions)
        {
            if (!string.Equals(_lastParsedCompilerOptions, compilerOptions, StringComparison.OrdinalIgnoreCase))
            {
                var splitArguments = CommandLineParser.SplitCommandLineIntoArguments(compilerOptions, removeHashComments: false);
                var commandLineArguments = ParseCommandLineArguments(splitArguments);
                _lastParsedCompilerOptions = compilerOptions;

                base.SetCommandLineArguments(commandLineArguments);
            }
        }
    }
}
