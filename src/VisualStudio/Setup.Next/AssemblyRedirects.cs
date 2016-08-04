// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.Setup;

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.EditorFeatures.Next.dll")]
[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.VisualStudio.CallHierarchy.Package.Definitions",
    GenerateCodeBase = false,
    PublicKeyToken = "31BF3856AD364E35",
    OldVersionLowerBound = "14.0.0.0",
    OldVersionUpperBound = "14.9.9.9",
    NewVersion = "15.0.0.0")]