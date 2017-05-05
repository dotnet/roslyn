// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.Setup;

[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.IntegrationTest.Setup.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.IntegrationTest.Utilities.dll")]
[assembly: ProvideRoslynBindingRedirection("Roslyn.Hosting.Diagnostics.dll")]

[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\Microsoft.Diagnostics.Runtime.dll")]
