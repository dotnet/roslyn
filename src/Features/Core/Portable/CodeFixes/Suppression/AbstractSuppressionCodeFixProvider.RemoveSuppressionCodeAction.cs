// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
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
            CodeActionOptionsProvider options,
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
                var formattingOptions = await documentOpt.GetSyntaxFormattingOptionsAsync(options, cancellationToken).ConfigureAwait(false);
                return PragmaRemoveAction.Create(suppressionTargetInfo, documentOpt, formattingOptions, diagnostic, fixer);
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
            : base(fixer, title: string.Format(FeaturesResources.Remove_Suppression_0, diagnostic.Id))
        {
            _diagnostic = diagnostic;
            _forFixMultipleContext = forFixMultipleContext;
        }

        public abstract RemoveSuppressionCodeAction CloneForFixMultipleContext();
        public abstract SyntaxTree SyntaxTreeToModify { get; }

        public override string EquivalenceKey => FeaturesResources.Remove_Suppression + DiagnosticIdForEquivalenceKey;
        protected override string DiagnosticIdForEquivalenceKey
            => _forFixMultipleContext ? string.Empty : _diagnostic.Id;
    }
}
