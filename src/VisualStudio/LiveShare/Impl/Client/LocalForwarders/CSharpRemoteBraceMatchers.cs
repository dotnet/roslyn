// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.CSharp.BraceMatching;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.LocalForwarders
{
    [ExportBraceMatcher(StringConstants.CSharpLspLanguageName)]
    internal class CSharpRemoteOpenCloseBraceBraceMatcher : OpenCloseBraceBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public CSharpRemoteOpenCloseBraceBraceMatcher() : base()
        {
        }
    }

    [ExportBraceMatcher(StringConstants.CSharpLspLanguageName)]
    internal class CSharpRemoteOpenCloseBracketBraceMatcher : OpenCloseBracketBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public CSharpRemoteOpenCloseBracketBraceMatcher() : base()
        {
        }
    }

    [ExportBraceMatcher(StringConstants.CSharpLspLanguageName)]
    internal class CSharpRemoteOpenCloseParenBraceMatcher : OpenCloseParenBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public CSharpRemoteOpenCloseParenBraceMatcher() : base()
        {
        }
    }

    [ExportBraceMatcher(StringConstants.CSharpLspLanguageName)]
    internal class CSharpRemoteLessThanGreaterThanBraceMatcher : LessThanGreaterThanBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public CSharpRemoteLessThanGreaterThanBraceMatcher() : base()
        {
        }
    }
}
