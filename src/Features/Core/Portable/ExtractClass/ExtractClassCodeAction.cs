// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.MoveMembers;

namespace Microsoft.CodeAnalysis.ExtractClass
{
    internal class ExtractClassCodeAction : AbstractMoveMembersCodeAction
    {
        public ExtractClassCodeAction(Document document, MoveMembersAnalysisResult analysisResult)
            : base(document, analysisResult)
        {
        }

        public override string Title => FeaturesResources.Extract_new_base_class;
    }
}
