﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options;

public partial class AutomationObject
{
    public int AutoComment
    {
        get { return GetBooleanOption(DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration); }
        set { SetBooleanOption(DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, value); }
    }
}
