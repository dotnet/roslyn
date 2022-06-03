// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Classification
{
    [DataContract]
    internal readonly record struct ClassificationOptions(
        [property: DataMember(Order = 0)] bool ClassifyReassignedVariables = false,
        [property: DataMember(Order = 1)] bool ColorizeRegexPatterns = true,
        [property: DataMember(Order = 2)] bool ColorizeJsonPatterns = true,
        [property: DataMember(Order = 3)] bool ForceFrozenPartialSemanticsForCrossProcessOperations = false)
    {
        public ClassificationOptions()
            : this(ClassifyReassignedVariables: false)
        {
        }

        public static readonly ClassificationOptions Default = new();
    }
}
