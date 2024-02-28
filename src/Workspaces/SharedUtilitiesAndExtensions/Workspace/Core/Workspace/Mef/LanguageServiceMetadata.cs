// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// MEF metadata class used for finding <see cref="ILanguageService"/> and <see cref="ILanguageServiceFactory"/> exports.
    /// </summary>
    internal class LanguageServiceMetadata(IDictionary<string, object> data) : ILanguageMetadata, ILayeredServiceMetadata
    {
        public string Language { get; } = (string)data[nameof(ExportLanguageServiceAttribute.Language)];
        public string ServiceType { get; } = (string)data[nameof(ExportLanguageServiceAttribute.ServiceType)];
        public string Layer { get; } = (string)data[nameof(ExportLanguageServiceAttribute.Layer)];

        public IReadOnlyList<string> WorkspaceKinds { get; } = (IReadOnlyList<string>)data[
#if CODE_STYLE
            "WorkspaceKinds"
#else
            nameof(ExportLanguageServiceAttribute.WorkspaceKinds)
#endif
        ];

        public IReadOnlyDictionary<string, object> Data { get; } = (IReadOnlyDictionary<string, object>)data;
    }
}
