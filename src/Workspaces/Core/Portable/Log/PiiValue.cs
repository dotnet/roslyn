// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// Represents telemetry data that's classified as personally identifiable information.
    /// </summary>
    internal sealed class PiiValue(object value)
    {
        public readonly object Value = value;

        public override string? ToString()
            => Value.ToString();
    }
}
