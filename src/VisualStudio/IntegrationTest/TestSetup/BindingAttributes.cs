// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell;

[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\Microsoft.VisualStudio.IntegrationTest.Setup.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\Microsoft.VisualStudio.IntegrationTest.IntegrationService.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\Roslyn.Hosting.Diagnostics.dll")]

[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\Microsoft.Diagnostics.Runtime.dll")]
