// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2231: Overload operator equals on overriding ValueType.Equals
    /// </summary>
    public abstract class CA2231CodeFixProviderBase : CodeFixProviderBase
    {
        protected const string LeftName = "left";
        protected const string RightName = "right";
        protected const string NotImplementedExceptionName = "System.NotImplementedException";

        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CA2231DiagnosticAnalyzer.RuleId);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.OverloadOperatorEqualsOnOverridingValueTypeEquals;
        }
    }
}
