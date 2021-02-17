// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation
{
    internal partial class SignatureHelpPresenter
    {
        private class SignatureHelpSource : ForegroundThreadAffinitizedObject, ISignatureHelpSource
        {
            public SignatureHelpSource(IThreadingContext threadingContext)
                : base(threadingContext)
            {
            }

            public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
            {
                AssertIsForeground();
                if (!session.Properties.TryGetProperty<SignatureHelpPresenterSession>(s_augmentSessionKey, out var presenterSession))
                {
                    return;
                }

                session.Properties.RemoveProperty(s_augmentSessionKey);
                presenterSession.AugmentSignatureHelpSession(signatures);
            }

            public ISignature GetBestMatch(ISignatureHelpSession session)
            {
                AssertIsForeground();
                return session.SelectedSignature;
            }

            public void Dispose()
            {
            }
        }
    }
}
