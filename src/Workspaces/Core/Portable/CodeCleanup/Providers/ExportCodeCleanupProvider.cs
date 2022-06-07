// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
    /// <summary>
    /// Specifies the exact type of the code cleanup exported.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
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
