// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        /// <summary>
        /// Base type for remove suppression code actions.
        /// </summary>
        internal abstract partial class RemoveSuppressionCodeAction : AbstractSuppressionCodeAction
        {
            private readonly Diagnostic _diagnostic;
            private readonly bool _forFixMultipleContext;

            public static async Task<RemoveSuppressionCodeAction> CreateAsync(
                SuppressionTargetInfo suppressionTargetInfo,
                Document documentOpt,
                Project project,
                Diagnostic diagnostic,
                AbstractSuppressionCodeFixProvider fixer,
                CancellationToken cancellationToken)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var attribute = diagnostic.GetSuppressionInfo(compilation).Attribute;
                if (attribute != null)
                {
                    return AttributeRemoveAction.Create(attribute, project, diagnostic, fixer);
                }
                else if (documentOpt != null && !SuppressionHelpers.IsSynthesizedExternalSourceDiagnostic(diagnostic))
                {
                    return PragmaRemoveAction.Create(suppressionTargetInfo, documentOpt, diagnostic, fixer);
                }
                else
                {
                    return null;
                }
            }

            protected RemoveSuppressionCodeAction(
                Diagnostic diagnostic,
                AbstractSuppressionCodeFixProvider fixer,
                bool forFixMultipleContext = false)
                : base(fixer, title: string.Format(FeaturesResources.RemoveSuppressionForId, diagnostic.Id))
            {
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
