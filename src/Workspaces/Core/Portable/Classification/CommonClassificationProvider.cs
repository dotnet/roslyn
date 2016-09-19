// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// The common classification provider base class used by C# and VB.
    /// </summary>
    internal abstract partial class CommonClassificationProvider : ClassificationProvider
    {
        public override async Task AddSyntacticClassificationsAsync(Document document, TextSpan span, ClassificationContext context, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            AddSyntacticClassifications(tree, span, workspace, context, cancellationToken);
        }

        public void AddSyntacticClassifications(SyntaxTree tree, TextSpan span, Workspace workspace, ClassificationContext context, CancellationToken cancellationToken)
        {
            try
            {
                AddSyntacticClassifications(tree, span, context, cancellationToken);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected abstract void AddSyntacticClassifications(SyntaxTree syntaxTree, TextSpan textSpan, ClassificationContext context, CancellationToken cancellationToken);

        public override async Task AddSemanticClassificationsAsync(Document document, TextSpan span, ClassificationContext context, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var model = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
            this.AddSemanticClassifications(model, span, workspace, context, cancellationToken);
        }

        public void AddSemanticClassifications(SemanticModel model, TextSpan span, Workspace workspace, ClassificationContext context, CancellationToken cancellationToken)
        {
            var semanticClassifiers = this.GetDefaultSemanticClassifiers();
            var extensionManager = workspace.Services.GetService<IExtensionManager>();
            var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(semanticClassifiers, c => c.SyntaxNodeTypes);
            var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(semanticClassifiers, c => c.SyntaxTokenKinds);

            try
            {
                SemanticClassificationWorker.Classify(workspace, model, span, context, getNodeClassifiers, getTokenClassifiers, cancellationToken);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public abstract ImmutableArray<ISemanticClassifier> GetDefaultSemanticClassifiers();
    }
}
