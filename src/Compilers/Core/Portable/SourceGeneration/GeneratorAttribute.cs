// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Place this attribute onto a type to cause it to be considered a source generator
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public sealed class GeneratorAttribute : Attribute
    {
        // PROTOTYPE: we don't know if we'll keep this, but for now it lets us re-use the analyzer discovery mechansim
    }
}
