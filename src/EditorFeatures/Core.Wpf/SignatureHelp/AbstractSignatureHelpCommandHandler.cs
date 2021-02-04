// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal abstract class AbstractSignatureHelpCommandHandler :
        ForegroundThreadAffinitizedObject
    {
        private readonly SignatureHelpControllerProvider _controllerProvider;

        public AbstractSignatureHelpCommandHandler(
            IThreadingContext threadingContext,
            SignatureHelpControllerProvider controllerProvider)
            : base(threadingContext)
        {
            _controllerProvider = controllerProvider;
        }

        protected bool TryGetController(EditorCommandArgs args, out Controller controller)
        {
            AssertIsForeground();

            // If args is `InvokeSignatureHelpCommandArgs` then sig help was explicitly invoked by the user and should
            // be shown whether or not the option is set.
            if (!(args is InvokeSignatureHelpCommandArgs) && !args.SubjectBuffer.GetFeatureOnOffOption(SignatureHelpOptions.ShowSignatureHelp))
            {
                controller = null;
                return false;
            }

            controller = _controllerProvider.GetController(args.TextView, args.SubjectBuffer);
            return controller is not null;
        }
    }
}
