// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
