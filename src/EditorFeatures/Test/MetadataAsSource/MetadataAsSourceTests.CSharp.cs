// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Test.Utilities;
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

                var extractedXMLFragment = DocumentationCommentUtilities.ExtractXMLFragment(docCommentText, "///");

                Assert.Equal(expectedXMLFragment, extractedXMLFragment);
            }
        }
    }
}
