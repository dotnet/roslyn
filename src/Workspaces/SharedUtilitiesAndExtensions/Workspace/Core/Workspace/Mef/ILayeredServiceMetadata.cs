// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Host.Mef;

internal interface ILayeredServiceMetadata
{
    IReadOnlyList<string> WorkspaceKinds { get; }
    string Layer { get; }
    string ServiceType { get; }
}
