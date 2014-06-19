using System.Collections.Generic;
using Microsoft.CodeAnalysis.FxCopDiagnosticFixers;
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

        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return SpecializedCollections.SingletonEnumerable(CA2231DiagnosticAnalyzer.RuleId);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.OverloadOperatorEqualsOnOverridingValueTypeEquals;
        }
    }
}
