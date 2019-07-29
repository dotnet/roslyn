// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines an attribute for LSP request handlers to map to LSP methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class ExportLspMethodAttribute : ExportAttribute, IRequestHandlerMetadata
    {
        public string MethodName { get; }

        public ExportLspMethodAttribute(string methodName) : base(typeof(IRequestHandler))
        {
            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException(nameof(methodName));
            }

            MethodName = methodName;
        }
    }
}
