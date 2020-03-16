// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.CodeAnalysis.Remote.Services.TodoComment
{
    internal class DescriptorInfo
    {
        public readonly string OptionText;
        public readonly ImmutableArray<TodoCommentDescriptor> Descriptors;

        public DescriptorInfo(string optionText, ImmutableArray<TodoCommentDescriptor> descriptors)
        {
            this.OptionText = optionText;
            this.Descriptors = descriptors;
        }
    }
}
