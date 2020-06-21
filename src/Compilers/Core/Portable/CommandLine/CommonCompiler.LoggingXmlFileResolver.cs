// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal sealed class LoggingXmlFileResolver : XmlFileResolver
        {
            private readonly TouchedFileLogger _loggerOpt;

            public LoggingXmlFileResolver(string baseDirectory, TouchedFileLogger logger)
                : base(baseDirectory)
            {
                _loggerOpt = logger;
            }

            protected override bool FileExists(string fullPath)
            {
                if (fullPath != null)
                {
                    _loggerOpt?.AddRead(fullPath);
                }

                return base.FileExists(fullPath);
            }
        }
    }
}
