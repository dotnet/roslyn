// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.NamespaceFileSync
{
    internal abstract class AbstractNamespaceFileSyncDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        //TODO: replace with real name
        private static readonly LocalizableString s_title =
            new LocalizableResourceString(nameof(AnalyzersResources.Use_auto_property),
                AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        //TODO: replace with your name
        protected AbstractNamespaceFileSyncDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseAutoPropertyDiagnosticId, CodeStyleOptions2.PreferAutoProperties, s_title, s_title)
        {

        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;


    }
}
