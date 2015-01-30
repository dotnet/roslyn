// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    /// <summary>
    /// CA1813: Avoid unsealed attributes
    /// </summary>
    public abstract class CA1813CodeFixProviderBase : CodeFixProviderBase
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CA1813DiagnosticAnalyzer.RuleId); }
        }

        protected sealed override string GetCodeFixDescription(Diagnostic diagnostic)
        {
            return FxCopFixersResources.AvoidUnsealedAttributesCodeFix;
        }
    }
}
