// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal abstract partial class RemoveSuppressionCodeAction : AbstractSuppressionCodeAction
        {
            private readonly Document _document;
            private readonly Diagnostic _diagnostic;
            private readonly bool _forFixMultipleContext;

            public static async Task<RemoveSuppressionCodeAction> CreateAsync(                
                SuppressionTargetInfo suppressionTargetInfo,
                Document document,
                Diagnostic diagnostic,
                AbstractSuppressionCodeFixProvider fixer,
                CancellationToken cancellationToken)
            {
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var attribute = diagnostic.GetSuppressionInfo(compilation).Attribute;
                if (attribute != null)
                {
                    return AttributeRemoveAction.Create(attribute, document, diagnostic, fixer);
                }
                else
                {
                    return PragmaRemoveAction.Create(suppressionTargetInfo, document, diagnostic, fixer);
                }
            }

            protected RemoveSuppressionCodeAction(
                Document document,
                Diagnostic diagnostic,
                AbstractSuppressionCodeFixProvider fixer,
                bool forFixMultipleContext = false)
                : base (fixer, title: string.Format(FeaturesResources.RemoveSuppressionForId, diagnostic.Id))
            {
                _document = document;
                _diagnostic = diagnostic;
                _forFixMultipleContext = forFixMultipleContext;
            }

            public abstract RemoveSuppressionCodeAction CloneForFixMultipleContext();
            public abstract SyntaxTree SyntaxTreeToModify { get; }

            public override string EquivalenceKey => FeaturesResources.RemoveSuppressionEquivalenceKeyPrefix + DiagnosticIdForEquivalenceKey;
            protected override string DiagnosticIdForEquivalenceKey =>
                _forFixMultipleContext ? string.Empty : _diagnostic.Id;
        }
    }
}
