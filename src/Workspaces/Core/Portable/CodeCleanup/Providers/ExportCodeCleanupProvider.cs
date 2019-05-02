// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
    /// <summary>
    /// Specifies the exact type of the code cleanup exported.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal class ExportCodeCleanupProvider : ExportAttribute
    {
        public string Name { get; }
        public IEnumerable<string> Languages { get; }

        public ExportCodeCleanupProvider(string name, params string[] languages)
            : base(typeof(ICodeCleanupProvider))
        {
            if (languages.Length == 0)
            {
                throw new ArgumentException("languages");
            }

            this.Name = name;
            this.Languages = languages ?? throw new ArgumentNullException(nameof(languages));
        }
    }
}
