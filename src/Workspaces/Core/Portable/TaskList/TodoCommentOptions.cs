// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.TaskList
{
    [DataContract]
    internal readonly record struct TaskListOptions
    {
        private static readonly ImmutableArray<string> s_defaultTokenList = ImmutableArray.Create("HACK:2", "TODO:2", "UNDONE:2", "UnresolvedMergeConflict:3");

        [DataMember]
        public ImmutableArray<string> TokenList { get; init; } = s_defaultTokenList;

        public TaskListOptions()
        {
        }

        public static readonly TaskListOptions Default = new();
    }
}
