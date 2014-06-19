// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal sealed class LoggingXmlFileResolver : XmlFileResolver
        {
            private readonly TouchedFileLogger logger;

            public LoggingXmlFileResolver(string baseDirectory, TouchedFileLogger logger)
                : base(baseDirectory)
            {
                this.logger = logger;
            }

            protected override bool FileExists(string fullPath)
            {
                if (logger != null && fullPath != null)
                {
                    this.logger.AddRead(fullPath);
                }

                return base.FileExists(fullPath);
            }
        }
    }
}
