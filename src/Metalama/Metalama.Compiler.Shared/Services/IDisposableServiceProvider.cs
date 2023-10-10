// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Microsoft.CodeAnalysis;

namespace Metalama.Compiler.Services;

public interface IDisposableServiceProvider : IServiceProvider
{
    void DisposeServices(Action<Diagnostic> reportDiagnostic);
}
