// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.CodeCleanup;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal abstract partial class AbstractMoveToNamespaceCodeAction
    {
        private class MoveItemsToNamespaceCodeAction : AbstractMoveToNamespaceCodeAction
        {
            public override string Title => FeaturesResources.Move_contents_to_namespace;

            public MoveItemsToNamespaceCodeAction(IMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult, CodeCleanupOptionsProvider cleanupOptions)
                : base(changeNamespaceService, analysisResult, cleanupOptions)
            {
            }
        }
    }
}
