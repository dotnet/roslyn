// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Base class for storing information decoded from early well-known custom attributes.
    /// </summary>
    /// <remarks>
    /// CONSIDER: Should we remove this class and let the sub-classes derived from WellKnownAttributeData?
    /// </remarks>
    internal abstract class EarlyWellKnownAttributeData : WellKnownAttributeData
    {
    }
}
