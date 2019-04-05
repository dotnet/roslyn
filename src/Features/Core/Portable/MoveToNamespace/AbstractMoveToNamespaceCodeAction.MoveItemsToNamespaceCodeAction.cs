// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal abstract partial class AbstractMoveToNamespaceCodeAction
    {
        private class MoveItemsToNamespaceCodeAction : AbstractMoveToNamespaceCodeAction
        {
            public override string Title => FeaturesResources.Move_contents_to_namespace;

            public MoveItemsToNamespaceCodeAction(IMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
                : base(changeNamespaceService, analysisResult)
            {
            }
        }
    }
}
