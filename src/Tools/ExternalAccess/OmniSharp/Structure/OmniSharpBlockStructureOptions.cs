﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Structure
{
    internal static class OmniSharpBlockStructureOptions
    {
        public static readonly PerLanguageOption<bool> ShowBlockStructureGuidesForCommentsAndPreprocessorRegions = (PerLanguageOption<bool>)BlockStructureOptions.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions;

        public static readonly PerLanguageOption<bool> ShowOutliningForCommentsAndPreprocessorRegions = (PerLanguageOption<bool>)BlockStructureOptions.ShowOutliningForCommentsAndPreprocessorRegions;
    }
}
