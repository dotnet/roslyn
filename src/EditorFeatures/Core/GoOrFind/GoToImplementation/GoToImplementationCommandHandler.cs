// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.GoOrFind;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.GoToImplementation;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.RoslynContentType)]
[Name(PredefinedCommandHandlerNames.GoToImplementation)]
[Order(Before = PredefinedCommandHandlerNames.LspGoToImplementation)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class GoToImplementationCommandHandler(GoToImplementationNavigationService navigationService)
    : AbstractGoOrFindCommandHandler<GoToImplementationCommandArgs>(navigationService);
