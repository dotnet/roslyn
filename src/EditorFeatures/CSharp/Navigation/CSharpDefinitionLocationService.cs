// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Navigation;

[ExportLanguageService(typeof(IDefinitionLocationService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal class CSharpDefinitionLocationService(IThreadingContext threadingContext, IStreamingFindUsagesPresenter streamingPresenter)
    : AbstractDefinitionLocationService(threadingContext, streamingPresenter);
