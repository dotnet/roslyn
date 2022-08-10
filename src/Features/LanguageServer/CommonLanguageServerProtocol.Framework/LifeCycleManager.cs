// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommonLanguageServerProtocol.Framework;

using System.Threading.Tasks;

public class LifeCycleManager<RequestContextType>
{
    private readonly AbstractLanguageServer<RequestContextType> _target;

    public LifeCycleManager(AbstractLanguageServer<RequestContextType> languageServerTarget)
    {
        _target = languageServerTarget;
    }

    public Task ExitAsync()
    {
        return _target.ExitAsync();
    }

    public Task ShutdownAsync()
    {
        return _target.ShutdownAsync();
    }
}
