// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1001: Types that own disposable fields should be disposable
    /// </summary>
    public abstract class CA1001CodeFixProviderBase : CodeFixProviderBase
    {
        protected const string NotImplementedExceptionName = "System.NotImplementedException";
        protected const string IDisposableName = "System.IDisposable";

        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CA1001DiagnosticAnalyzer.RuleId);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.ImplementIDisposableInterface;
        }
    }
}