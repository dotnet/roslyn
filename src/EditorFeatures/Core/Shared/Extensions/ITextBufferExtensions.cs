// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class ITextBufferExtensions
    {
        internal static bool GetFeatureOnOffOption(this ITextBuffer buffer, Option<bool> option)
        {
            var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document != null)
            {
                return document.Project.Solution.Options.GetOption(option);
            }

            return option.DefaultValue;
        }

        internal static bool GetFeatureOnOffOption(this ITextBuffer buffer, PerLanguageOption<bool> option)
        {
            // Add a FailFast to help diagnose 984249.  Hopefully this will let us know what the issue is.
            try
            {
                var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

                if (document != null)
                {
                    return document.Project.Solution.Options.GetOption(option, document.Project.Language);
                }

                return option.DefaultValue;
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
