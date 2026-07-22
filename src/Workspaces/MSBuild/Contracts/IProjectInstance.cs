// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// RPC methods for MSBuild's <c>ProjectInstance</c> object.
/// </summary>
internal interface IProjectInstance : IDisposable
{
    DiagnosticLogItem[] GetDiagnosticLogItems();
    int[] GetItems(string itemType);
    string GetPropertyValue(string propertyName);
    string ExpandString(string value);
}
