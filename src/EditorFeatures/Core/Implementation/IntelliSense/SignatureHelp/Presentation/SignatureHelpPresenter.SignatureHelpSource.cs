// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
