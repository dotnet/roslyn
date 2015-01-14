// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
