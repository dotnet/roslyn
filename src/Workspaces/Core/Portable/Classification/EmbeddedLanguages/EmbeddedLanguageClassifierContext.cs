// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal struct EmbeddedLanguageClassificationContext
    {
        internal readonly HostWorkspaceServices WorkspaceServices;
        internal readonly ClassificationOptions Options;

        private readonly ArrayBuilder<ClassifiedSpan> _result;

        public Project? Project { get; }

        /// <summary>
        /// The string or character token to classify.
        /// </summary>
        public SyntaxToken SyntaxToken { get; }

        /// <summary>
        /// SemanticModel that <see cref="SyntaxToken"/> is contained in.
        /// </summary>
        public SemanticModel SemanticModel { get; }

        public CancellationToken CancellationToken { get; }

        internal EmbeddedLanguageClassificationContext(
            HostWorkspaceServices workspaceServices,
            Project? project,
            SemanticModel semanticModel,
            SyntaxToken syntaxToken,
            ClassificationOptions options,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            WorkspaceServices = workspaceServices;
            Project = project;
            SemanticModel = semanticModel;
            SyntaxToken = syntaxToken;
            Options = options;
            _result = result;
            CancellationToken = cancellationToken;
        }

        public void AddClassification(string classificationType, TextSpan span)
            => _result.Add(new ClassifiedSpan(classificationType, span));
    }
}
