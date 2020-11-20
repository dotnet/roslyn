﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.AddImports;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AddImports
{
    [Export]
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.AddImportsPaste)]
    // Order is important here, this command needs to execute before PasteTracking
    // since it may modify the pasted span. Paste tracking dismisses if 
    // the span is modified. It doesn't need to be before FormatDocument, but
    // this helps the order of execution be more constant in case there 
    // are problems that arise. This command will always execute the next
    // command before doing operations.
    [Order(After = PredefinedCommandHandlerNames.PasteTrackingPaste)]
    [Order(Before = PredefinedCommandHandlerNames.FormatDocument)]
    internal class CSharpAddImportsPasteCommandHandler : AbstractAddImportsPasteCommandHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpAddImportsPasteCommandHandler(IThreadingContext threadingContext) : base(threadingContext)
        {
        }

        public override string DisplayName => CSharpEditorResources.Add_Missing_Usings_on_Paste;
        protected override string DialogText => CSharpEditorResources.Adding_missing_usings;
    }
}
