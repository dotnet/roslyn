// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Enables the managment of the server lifecycle from outside of the AbstractlanguageServer object.
/// This allows implementors an entry-point to do things like log error messages and
/// clean up things related to server exit and shutdown.
/// </summary>
public interface ILifeCycleManager
{
    Task ExitAsync();

    Task ShutdownAsync(string message = "Shutting down");
}
