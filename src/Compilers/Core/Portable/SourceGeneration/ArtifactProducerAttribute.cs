// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Place this attribute onto a type to cause it to be considered an artifact producer.  Without this calls to <see
    /// cref="AnalysisContext.TryGetArtifactContext"/> will throw.  With this, similar calls may succeed or not
    /// depending on if the caller is used in a context where artifact production is supported or not. In general that
    /// will only be when a compiler is invoked with the <c>generatedartifactsout</c> argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ArtifactProducerAttribute : Attribute
    {
    }
}
