// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

/// <summary>
/// Note: we very intentionally place ourselves in the editors 'lexical' bucket (and not 'syntactic' bucket).  See
/// comments on <see cref="AbstractClassificationTypeMap"/> for more details.
/// </summary>
[Export]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class SyntacticClassificationTypeMap(IClassificationTypeRegistryService registryService)
    : AbstractClassificationTypeMap(registryService, ClassificationLayer.Lexical)
{
}
