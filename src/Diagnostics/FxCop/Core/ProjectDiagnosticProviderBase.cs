// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FxCopRules.DiagnosticProviders
{
    /// <summary>
    /// Subtype this class to provide project diagnostics about a given <see cref="Project"/>.
    /// </summary>
    public abstract class ProjectDiagnosticProviderBase : IDiagnosticProvider
    {
        /// <summary>
        /// Returns true if the specified kinds are supported by this provider.
        /// </summary>
        bool IDiagnosticProvider.IsSupported(DiagnosticCategory category)
        {
            return category == DiagnosticCategory.Project;
        }

        protected abstract Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(Project project, CancellationToken cancellationToken);

        async Task<IEnumerable<Diagnostic>> IDiagnosticProvider.GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return await GetDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
        }

        Task<IEnumerable<Diagnostic>> IDiagnosticProvider.GetSemanticDiagnosticsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        Task<IEnumerable<Diagnostic>> IDiagnosticProvider.GetSyntaxDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public abstract IEnumerable<DiagnosticDescriptor> GetSupportedDiagnostics();
    }
}
