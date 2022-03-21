// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.TodoComments
{
    [DataContract]
    internal readonly record struct TodoCommentOptions(
        [property: DataMember(Order = 0)] string TokenList = "")
    {
        public TodoCommentOptions()
            : this(TokenList: "")
        {
        }

        public static readonly TodoCommentOptions Default = new();
    }
}
