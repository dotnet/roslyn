// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal sealed class LoggingStrongNameProvider : DesktopStrongNameProvider
        {
            private readonly TouchedFileLogger _loggerOpt;

            public LoggingStrongNameProvider(ImmutableArray<string> keyFileSearchPaths, TouchedFileLogger logger)
                : base(keyFileSearchPaths)
            {
                _loggerOpt = logger;
            }

            internal override bool FileExists(string fullPath)
            {
                if (fullPath != null)
                {
                    _loggerOpt?.AddRead(fullPath);
                }

                return base.FileExists(fullPath);
            }

            internal override byte[] ReadAllBytes(string fullPath)
            {
                _loggerOpt?.AddRead(fullPath);
                return base.ReadAllBytes(fullPath);
            }
        }
    }
}
