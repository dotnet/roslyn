// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Fix all occurrences code action.
    /// </summary>
    internal partial class FixAllCodeAction : CodeAction
    {
        private readonly FixAllContext fixAllContext;
        private readonly FixAllProvider fixAllProvider;
        private static readonly HashSet<string> predefinedCodeFixProviderNames = GetPredefinedCodeFixProviderNames();

        internal FixAllCodeAction(FixAllContext fixAllContext, FixAllProvider fixAllProvider)
        {
            this.fixAllContext = fixAllContext;
            this.fixAllProvider = fixAllProvider;
        }

        public override string Title
        {
            get
            {
                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Document:
                        return FeaturesResources.FixAllTitle_Document;
                    case FixAllScope.Project:
                        return FeaturesResources.FixAllTitle_Project;
                    case FixAllScope.Solution:
                        return FeaturesResources.FixAllTitle_Solution;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            FixAllLogger.LogContext(this.fixAllContext, IsInternalCodeFixProvider(this.fixAllContext.CodeFixProvider));

            var service = this.fixAllContext.Project.Solution.Workspace.Services.GetService<IFixAllGetFixesService>();
            return await service.GetFixAllOperationsAsync(this.fixAllProvider, this.fixAllContext).ConfigureAwait(false);
        }

        private static bool IsInternalCodeFixProvider(CodeFixProvider fixer)
        {
            var exportAttributes = fixer.GetType().GetCustomAttributes(typeof(ExportCodeFixProviderAttribute), false);
            if (exportAttributes != null && exportAttributes.Length > 0)
            {
                var exportAttribute = (ExportCodeFixProviderAttribute)exportAttributes[0];
                return predefinedCodeFixProviderNames.Contains(exportAttribute.Name);
            }

            return false;
        }

        private static HashSet<string> GetPredefinedCodeFixProviderNames()
        {
            var names = new HashSet<string>();

            var fields = typeof(PredefinedCodeFixProviderNames).GetFields();
            foreach (var field in fields)
            {
                if (field.IsStatic)
                {
                    names.Add((string)field.GetRawConstantValue());
                }
            }

            return names;
        }
    }
}
