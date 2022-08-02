// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommonLanguageServerProtocol.Framework;

public class LifeCycleManager<RequestContextType> : ILifeCycleManager
{
    private readonly LanguageServer<RequestContextType> _target;

    public LifeCycleManager(LanguageServer<RequestContextType> languageServerTarget)
    {
        _target = languageServerTarget;
    }

    public void Exit()
    {
        _target.Exit();
    }

    public void Shutdown()
    {
        _target.Shutdown();
    }
}
