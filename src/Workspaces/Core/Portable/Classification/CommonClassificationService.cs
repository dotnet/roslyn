// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Extensions;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract partial class CommonClassificationService : ClassificationService
    {
        protected CommonClassificationService()
        {
        }

        public override ImmutableArray<ClassifiedSpan> GetLexicalClassifications(SourceText text, TextSpan span, CancellationToken cancellationToken)
        {
            var result = ImmutableArray<ClassifiedSpan>.Empty;
            var classifications = SharedPools.Default<List<ClassifiedSpan>>().Allocate();

            try
            {
                this.AddLexicalClassifications(text, span, classifications, cancellationToken);
                result = classifications.ToImmutableArray();
            }
            finally
            {
                SharedPools.Default<List<ClassifiedSpan>>().ClearAndFree(classifications);
            }

            return result;
        }

        protected abstract void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken);

        public override async Task<ImmutableArray<ClassifiedSpan>> GetSyntacticClassificationsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return GetSyntacticClassifications(tree, span, workspace, cancellationToken);
        }

        public ImmutableArray<ClassifiedSpan> GetSyntacticClassifications(SyntaxTree tree, TextSpan span, Workspace workspace, CancellationToken cancellationToken)
        {
            var syntaxClassifiers = this.GetDefaultSyntaxClassifiers();

            var extensionManager = workspace.Services.GetService<IExtensionManager>();
            var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(syntaxClassifiers, c => c.SyntaxNodeTypes);
            var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(syntaxClassifiers, c => c.SyntaxTokenKinds);

            var result = ImmutableArray<ClassifiedSpan>.Empty;
            var classifications = SharedPools.Default<List<ClassifiedSpan>>().Allocate();

            try
            {
                AddSyntacticClassifications(tree, span, classifications, cancellationToken);
                result = classifications.ToImmutableArray();
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
            finally
            {
                SharedPools.Default<List<ClassifiedSpan>>().ClearAndFree(classifications);
            }

            return result;
        }

        protected virtual void AddSyntacticClassifications(SyntaxTree syntaxTree, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
        }

        public override async Task<ImmutableArray<ClassifiedSpan>> GetSemanticClassificationsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var model = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
            return this.GetSemanticClassifications(model, span, workspace, cancellationToken);
        }

        public ImmutableArray<ClassifiedSpan> GetSemanticClassifications(SemanticModel model, TextSpan span, Workspace workspace, CancellationToken cancellationToken)
        {
            var syntaxClassifiers = this.GetDefaultSyntaxClassifiers();

            var extensionManager = workspace.Services.GetService<IExtensionManager>();
            var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(syntaxClassifiers, c => c.SyntaxNodeTypes);
            var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(syntaxClassifiers, c => c.SyntaxTokenKinds);

            var result = ImmutableArray<ClassifiedSpan>.Empty;
            var classifications = SharedPools.Default<List<ClassifiedSpan>>().Allocate();

            try
            {
                Worker.Classify(workspace, model, span, classifications, getNodeClassifiers, getTokenClassifiers, cancellationToken);
                result = classifications.ToImmutableArray();
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
            finally
            {
                SharedPools.Default<List<ClassifiedSpan>>().ClearAndFree(classifications);
            }

            return result;
        }

        public abstract IEnumerable<ISyntaxClassifier> GetDefaultSyntaxClassifiers();
    }
}
