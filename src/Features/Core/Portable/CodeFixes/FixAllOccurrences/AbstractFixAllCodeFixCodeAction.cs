// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Fix all code action for a code action registered by a <see cref="CodeFixProvider"/>.
    /// </summary>
    internal abstract class AbstractFixAllCodeFixCodeAction : AbstractFixAllCodeAction
    {
        private static readonly HashSet<string> s_predefinedCodeFixProviderNames = GetPredefinedCodeFixProviderNames();

        protected AbstractFixAllCodeFixCodeAction(
            IFixAllState fixAllState, bool showPreviewChangesDialog)
            : base(fixAllState, showPreviewChangesDialog)
        {
        }

        protected override IFixAllContext CreateFixAllContext(IFixAllState fixAllState, IProgressTracker progressTracker, CancellationToken cancellationToken)
            => new FixAllContext((FixAllState)fixAllState, progressTracker, cancellationToken);

        protected override bool IsInternalProvider(IFixAllState fixAllState)
        {
            var exportAttributes = fixAllState.Provider.GetType().GetTypeInfo().GetCustomAttributes(typeof(ExportCodeFixProviderAttribute), false);
            if (exportAttributes?.Any() == true)
            {
                var exportAttribute = (ExportCodeFixProviderAttribute)exportAttributes.First();
                return !string.IsNullOrEmpty(exportAttribute.Name)
                    && s_predefinedCodeFixProviderNames.Contains(exportAttribute.Name);
            }

            return false;
        }

        private static HashSet<string> GetPredefinedCodeFixProviderNames()
        {
            var names = new HashSet<string>();

            var fields = typeof(PredefinedCodeFixProviderNames).GetTypeInfo().DeclaredFields;
            foreach (var field in fields)
            {
                if (field.IsStatic)
                {
                    names.Add((string)field.GetValue(null)!);
                }
            }

            return names;
        }
    }
}
