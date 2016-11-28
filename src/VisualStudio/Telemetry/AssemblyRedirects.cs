// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.Setup;

// This assembly doesn't contain any components that are referencable or need binding redirects for API compatibility.
// This is purely so experimental versions of the VSIX can be installed and load the right assembly.
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.Telemetry.dll")]
