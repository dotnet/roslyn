// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource
{
    public partial class MetadataAsSourceTests
    {
        public class CSharp
        {
            [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
            public void ExtractXMLFromDocComment()
            {
                var docCommentText = @"/// <summary>
/// I am the very model of a modern major general.
/// </summary>";

                var expectedXMLFragment = @" <summary>
 I am the very model of a modern major general.
 </summary>";

                var extractedXMLFragment = DocumentationCommentUtilities.ExtractXMLFragment(docCommentText);

                Assert.Equal(expectedXMLFragment, extractedXMLFragment);
            }
        }
    }
}
