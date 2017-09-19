using System;
using Microsoft.Test.Apex;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public class TestOperationsConfiguration : OperationsConfiguration
    {
        protected override void OnOperationsCreated(Operations operations)
        {
            DelayedAssertionVerifierSink verifier = operations.Get<DelayedAssertionVerifierSink>();
        }

        public Action<String> FailureAction { get; set; }

        protected override Type Verifier => typeof(DelayedAssertionVerifierSink);
    }
}
