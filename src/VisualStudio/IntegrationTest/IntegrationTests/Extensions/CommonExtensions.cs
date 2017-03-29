// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions
{
    public static partial class CommonExtensions
    {
        public static void ExecuteCommand(this AbstractIntegrationTest test, string commandName, string argument = "")
            => test.VisualStudio.Instance.ExecuteCommand(commandName, argument);

        public static void WaitForAsyncOperations(this AbstractIntegrationTest test, params string[] featuresToWaitFor)
            => test.VisualStudio.Instance.VisualStudioWorkspace.WaitForAsyncOperations(string.Join(";", featuresToWaitFor));

        public static void InvokeCompletionList(AbstractIntegrationTest test)
        {
            test.ExecuteCommand(WellKnownCommandNames.Edit_ListMembers);
            test.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
        }
    }
}
