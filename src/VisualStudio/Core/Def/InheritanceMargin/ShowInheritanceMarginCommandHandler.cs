// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(ContentTypeNames.VisualBasicContentType)]
[Name(nameof(ShowInheritanceMarginCommandHandler))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ShowInheritanceMarginCommandHandler(IGlobalOptionService globalOptions) : ICommandHandler<ShowInheritanceMarginCommandArgs>
{
    public string DisplayName => ServicesVSResources.Show_Inheritance;

    public CommandState GetCommandState(ShowInheritanceMarginCommandArgs args)
    {
        var language = args.SubjectBuffer.GetLanguageName();
        if (language is null)
        {
            return CommandState.Unspecified;
        }

        var isChecked = globalOptions.GetOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, language) ?? true;
        return new CommandState(isAvailable: true, isChecked);
    }

    public bool ExecuteCommand(ShowInheritanceMarginCommandArgs args, CommandExecutionContext context)
    {
        var language = args.SubjectBuffer.GetLanguageName();
        if (language is null)
        {
            return false;
        }

        var current = globalOptions.GetOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, language) ?? true;
        globalOptions.SetGlobalOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, language, !current);
        return true;
    }
}
