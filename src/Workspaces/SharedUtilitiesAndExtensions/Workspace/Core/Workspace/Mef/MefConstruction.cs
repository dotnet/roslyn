// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Host.Mef;

internal static class MefConstruction
{
    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
    internal const string FactoryMethodMessage = "This factory method only provides services for the MEF export provider.";
}
