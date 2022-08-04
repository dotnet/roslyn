// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLanguageServerProtocol.Framework;

/// <summary>
/// Used to manage server lifecycle.
/// </summary>
public interface ILifeCycleManager
{
    /// <summary>
    /// Shutdown the LanguageServer, for example because "shutdown" was called.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Exit the LanguageServer, for example because "exit" was called.
    /// </summary>
    void Exit();
}
