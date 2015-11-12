// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.Setup;

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Scripting.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.Scripting.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Scripting.VisualBasic.dll")]

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.InteractiveEditorFeatures.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.InteractiveEditorFeatures.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.InteractiveEditorFeatures.dll")]

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.InteractiveFeatures.dll")]

[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.InteractiveServices.dll")]

[assembly: ProvideRoslynBindingRedirection("InteractiveHost.exe")]
