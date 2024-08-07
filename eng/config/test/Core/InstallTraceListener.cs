// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Roslyn.Test.Utilities;

internal sealed class InitializeTestModule
{
    [ModuleInitializer]
    internal static void Initializer()
    {
        RuntimeHelpers.RunModuleConstructor(typeof(TestBase).Module.ModuleHandle);
    }
}
