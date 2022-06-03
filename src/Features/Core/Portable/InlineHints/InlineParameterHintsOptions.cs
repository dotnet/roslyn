// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.InlineHints
{
    [DataContract]
    internal readonly record struct InlineParameterHintsOptions(
        [property: DataMember(Order = 0)] bool EnabledForParameters = false,
        [property: DataMember(Order = 1)] bool ForLiteralParameters = true,
        [property: DataMember(Order = 2)] bool ForIndexerParameters = true,
        [property: DataMember(Order = 3)] bool ForObjectCreationParameters = true,
        [property: DataMember(Order = 4)] bool ForOtherParameters = false,
        [property: DataMember(Order = 5)] bool SuppressForParametersThatDifferOnlyBySuffix = true,
        [property: DataMember(Order = 6)] bool SuppressForParametersThatMatchMethodIntent = true,
        [property: DataMember(Order = 7)] bool SuppressForParametersThatMatchArgumentName = true)
    {
        public InlineParameterHintsOptions()
            : this(EnabledForParameters: false)
        {
        }

        public static readonly InlineParameterHintsOptions Default = new();
    }
}
