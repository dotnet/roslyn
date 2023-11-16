// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CodeLens
{
    /// <summary>
    /// Represents the result of a FindReferences Count operation.
    /// </summary>
    /// <param name="Count">Represents the number of references to a given symbol.</param>
    /// <param name="IsCapped">Represents if the count is capped by a certain maximum.</param>
    [DataContract]
    internal readonly record struct ReferenceCount(
        [property: DataMember(Order = 0)] int Count,
        [property: DataMember(Order = 1)] bool IsCapped,
        [property: DataMember(Order = 2)] string Version)
    {
        public string GetDescription()
        {
            var referenceWord = Count == 1
                ? FeaturesResources._0_reference_unquoted
                : FeaturesResources._0_references_unquoted;

            var description = string.Format(referenceWord, GetCappedReferenceCountString());
            return description;
        }

        public string GetToolTip(string? codeElementKind)
            => string.Format(FeaturesResources.This_0_has_1_references, codeElementKind, GetCappedReferenceCountString());

        private string GetCappedReferenceCountString() => $"{Count}{(IsCapped ? "+" : string.Empty)}";
    }
}
