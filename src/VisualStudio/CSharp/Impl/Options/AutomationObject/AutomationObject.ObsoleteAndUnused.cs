// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options;

public partial class AutomationObject
{
    /// <summary>
    /// Unused.  But kept around for back compat.  Note this option is not about
    /// turning warning into errors.  It's about an aspect of 'remove unused using'
    /// functionality we don't support anymore.  Namely whether or not 'remove unused
    /// using' should warn if you have any build errors as that might mean we 
    /// remove some usings inappropriately.
    /// </summary>
    public int WarnOnBuildErrors
    {
        get { return 0; }
        set { }
    }

    public int ShowKeywords
    {
        get { return 0; }
        set { }
    }

    [Obsolete("ClosedFileDiagnostics has been deprecated")]
    public int ClosedFileDiagnostics
    {
        get { return 0; }
        set { }
    }

    [Obsolete("CSharpClosedFileDiagnostics has been deprecated")]
    public int CSharpClosedFileDiagnostics
    {
        get { return 0; }
        set { }
    }

    [Obsolete("Use SnippetsBehavior instead")]
    public int ShowSnippets
    {
        get
        {
            return GetOption(CompletionOptionsStorage.SnippetsBehavior) == SnippetsRule.AlwaysInclude
                ? 1 : 0;
        }

        set
        {
            if (value == 0)
            {
                SetOption(CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.NeverInclude);
            }
            else
            {
                SetOption(CompletionOptionsStorage.SnippetsBehavior, SnippetsRule.AlwaysInclude);
            }
        }
    }

    [Obsolete("Use Style_UseImplicitTypeWherePossible, Style_UseImplicitTypeWhereApparent or Style_UseImplicitTypeForIntrinsicTypes", error: true)]
    public int Style_UseVarWhenDeclaringLocals
    {
        get { return 0; }
        set { }
    }
}
