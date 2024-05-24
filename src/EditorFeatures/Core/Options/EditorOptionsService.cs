// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Aggregates services necessary to retrieve editor options.
/// </summary>
[Export(typeof(EditorOptionsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorOptionsService(IGlobalOptionService globalOptions, IEditorOptionsFactoryService factory, IIndentationManagerService indentationManager)
{
    public readonly IGlobalOptionService GlobalOptions = globalOptions;
    public readonly IEditorOptionsFactoryService Factory = factory;
    public readonly IIndentationManagerService IndentationManager = indentationManager;
}
