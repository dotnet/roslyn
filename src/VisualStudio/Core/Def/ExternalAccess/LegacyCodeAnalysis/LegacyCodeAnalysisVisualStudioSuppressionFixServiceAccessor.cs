// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.Suppression;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis
{
    [Export(typeof(ILegacyCodeAnalysisVisualStudioSuppressionFixServiceAccessor))]
    [Shared]
    internal sealed class LegacyCodeAnalysisVisualStudioSuppressionFixServiceAccessor
        : ILegacyCodeAnalysisVisualStudioSuppressionFixServiceAccessor
    {
        private readonly IVisualStudioSuppressionFixService _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LegacyCodeAnalysisVisualStudioSuppressionFixServiceAccessor(IVisualStudioSuppressionFixService implementation)
            => _implementation = implementation;

        public bool AddSuppressions(IVsHierarchy projectHierarchyOpt)
            => _implementation.AddSuppressions(projectHierarchyOpt);

        public bool AddSuppressions(bool selectedErrorListEntriesOnly, bool suppressInSource, IVsHierarchy projectHierarchyOpt)
            => _implementation.AddSuppressions(selectedErrorListEntriesOnly, suppressInSource, projectHierarchyOpt);

        public bool RemoveSuppressions(bool selectedErrorListEntriesOnly, IVsHierarchy projectHierarchyOpt)
            => _implementation.RemoveSuppressions(selectedErrorListEntriesOnly, projectHierarchyOpt);
    }
}
