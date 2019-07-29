// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    internal abstract partial class AbstractMoveToNamespaceCodeAction
    {
        private class MoveTypeToNamespaceCodeAction : AbstractMoveToNamespaceCodeAction
        {
            public override string Title => FeaturesResources.Move_to_namespace;

            public MoveTypeToNamespaceCodeAction(IMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
                : base(changeNamespaceService, analysisResult)
            {
            }
        }
    }
}
