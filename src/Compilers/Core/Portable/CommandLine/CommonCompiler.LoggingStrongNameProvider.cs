// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal sealed class LoggingStrongNameFileSystem : StrongNameFileSystem
        {
            private readonly TouchedFileLogger? _loggerOpt;

            public LoggingStrongNameFileSystem(TouchedFileLogger? logger, string? customTempPath)
                : base(customTempPath)
            {
                _loggerOpt = logger;
            }

            internal override bool FileExists(string? fullPath)
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
