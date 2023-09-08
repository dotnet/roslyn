// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote.ProjectSystem;

/// <summary>
/// A marker interface that represents a loaded project from the <see cref="ILanguageServerProjectSystemService"/>. At this point you can't do anything with this other
/// than dispose it, which indicates you want the project to be unloaded.
/// </summary>
[RpcMarshalable]
internal interface ILoadedProjectSystemProject : IDisposable
{
}
