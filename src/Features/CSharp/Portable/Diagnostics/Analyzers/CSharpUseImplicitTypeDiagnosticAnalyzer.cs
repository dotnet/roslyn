// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseImplicitTypeDiagnosticAnalyzer : CSharpTypeStyleDiagnosticAnalyzerBase
    {
        private static readonly LocalizableString s_Title =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.Use_implicit_type), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        private static readonly LocalizableString s_Message =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.use_var_instead_of_explicit_type), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        protected override CSharpTypeStyleHelper Helper => CSharpUseImplicitTypeHelper.Instance;

        public CSharpUseImplicitTypeDiagnosticAnalyzer()
            : base(diagnosticId: IDEDiagnosticIds.UseImplicitTypeDiagnosticId,
                   title: s_Title,
                   message: s_Message)
        {
        }
    }
}
