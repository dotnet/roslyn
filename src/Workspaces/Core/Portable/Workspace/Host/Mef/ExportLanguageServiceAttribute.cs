// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// Use this attribute to declare a <see cref="ILanguageService"/> implementation for inclusion in a MEF-based workspace.
    /// </summary>
    /// <remarks>
    /// Declares a <see cref="ILanguageService"/> implementation for inclusion in a MEF-based workspace.
    /// </remarks>
    /// <param name="type">The type that will be used to retrieve the service from a <see cref="HostLanguageServices"/>.</param>
    /// <param name="language">The language that the service is target for; <see cref="LanguageNames.CSharp"/>, etc.</param>
    /// <param name="layer">The layer that the service is specified for; <see cref="ServiceLayer.Default"/>, etc.</param>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportLanguageServiceAttribute(Type type, string language, string layer = ServiceLayer.Default) : ExportAttribute(typeof(ILanguageService))
    {
        /// <summary>
        /// The assembly qualified name of the service's type.
        /// </summary>
        public string ServiceType { get; } = LayeredServiceUtilities.GetAssemblyQualifiedServiceTypeName(type, nameof(type));

        /// <summary>
        /// The language that the service is target for. Specify a value from <see cref="LanguageNames"/>, or other language name.
        /// </summary>
        public string Language { get; } = language ?? throw new ArgumentNullException(nameof(language));

        /// <summary>
        /// The layer that the service is specified for. Specify a value from <see cref="ServiceLayer"/>.
        /// </summary>
        public string Layer { get; } = layer ?? throw new ArgumentNullException(nameof(layer));

        /// <summary>
        /// <see cref="WorkspaceKind"/>s that the service is specified for.
        /// If non-empty the service is only exported for the listed workspace kinds and <see cref="Layer"/> is not applied,
        /// unless <see cref="Layer"/> is <see cref="ServiceLayer.Test"/> in which case the export overrides all other exports.
        /// </summary>
        internal IReadOnlyList<string> WorkspaceKinds { get; } = [];

        internal ExportLanguageServiceAttribute(Type type, string language, string[] workspaceKinds)
            : this(type, language)
        {
            WorkspaceKinds = workspaceKinds;
        }
    }
}
