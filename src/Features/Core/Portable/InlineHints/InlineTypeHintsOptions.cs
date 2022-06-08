// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.InlineHints
{
    [DataContract]
    internal readonly record struct InlineTypeHintsOptions(
        [property: DataMember(Order = 0)] bool EnabledForTypes = false,
        [property: DataMember(Order = 1)] bool ForImplicitVariableTypes = true,
        [property: DataMember(Order = 2)] bool ForLambdaParameterTypes = true,
        [property: DataMember(Order = 3)] bool ForImplicitObjectCreation = true)
    {
        public InlineTypeHintsOptions()
            : this(EnabledForTypes: false)
        {
        }

        public static readonly InlineTypeHintsOptions Default = new();
    }
}
