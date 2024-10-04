﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.MoveToNamespace;

internal abstract partial class AbstractMoveToNamespaceCodeAction
{
    private sealed class MoveTypeToNamespaceCodeAction(IMoveToNamespaceService changeNamespaceService, MoveToNamespaceAnalysisResult analysisResult)
        : AbstractMoveToNamespaceCodeAction(changeNamespaceService, analysisResult)
    {
        public override string Title => FeaturesResources.Move_to_namespace;
    }
}
