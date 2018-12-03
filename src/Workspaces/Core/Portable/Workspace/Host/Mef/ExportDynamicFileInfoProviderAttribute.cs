﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// Use this attribute to declare a <see cref="IDynamicFileInfoProvider"/> implementation for MEF
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportDynamicFileInfoProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// file extensions this <see cref="IDynamicFileInfoProvider"/> can handle such as cshtml
        /// 
        /// match will be done by <see cref="StringComparer.OrdinalIgnoreCase"/>
        /// </summary>
        public IEnumerable<string> Extensions { get; }

        public ExportDynamicFileInfoProviderAttribute(params string[] extensions)
            : base(typeof(IDynamicFileInfoProvider))
        {
            if (extensions?.Length == 0)
            {
                throw new ArgumentException(nameof(extensions));
            }

            Extensions = extensions;
        }
    }
}
