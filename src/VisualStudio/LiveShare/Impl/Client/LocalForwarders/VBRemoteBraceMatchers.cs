// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.LocalForwarders
{
    [ExportBraceMatcher(StringConstants.VBLspLanguageName)]
    internal class VBRemoteOpenCloseBraceBraceMatcher : OpenCloseBraceBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public VBRemoteOpenCloseBraceBraceMatcher() : base()
        {
        }
    }

    [ExportBraceMatcher(StringConstants.VBLspLanguageName)]
    internal class VBRemoteOpenCloseParenBraceMatcher : OpenCloseParenBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public VBRemoteOpenCloseParenBraceMatcher() : base()
        {
        }
    }

    [ExportBraceMatcher(StringConstants.VBLspLanguageName)]
    internal class VBRemoteLessThanGreaterThanBraceMatcher : LessThanGreaterThanBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public VBRemoteLessThanGreaterThanBraceMatcher() : base()
        {
        }
    }
}
