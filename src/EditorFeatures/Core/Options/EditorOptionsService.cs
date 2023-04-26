// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Aggregates services necessary to retrieve editor options.
/// </summary>
[Export(typeof(EditorOptionsService)), Shared]
internal sealed class EditorOptionsService
{
    public readonly IGlobalOptionService GlobalOptions;
    public readonly IEditorOptionsFactoryService Factory;
    public readonly IIndentationManagerService IndentationManager;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EditorOptionsService(IGlobalOptionService globalOptions, IEditorOptionsFactoryService factory, IIndentationManagerService indentationManager)
    {
        GlobalOptions = globalOptions;
        Factory = factory;
        IndentationManager = indentationManager;
    }
}
