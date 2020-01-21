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

        internal static bool TryGetWorkspace(this ITextBuffer buffer, out Workspace workspace)
            => Workspace.TryGetWorkspace(buffer.AsTextContainer(), out workspace);

        /// <summary>
        /// Checks if a buffer supports refactorings.
        /// </summary>
        internal static bool SupportsRefactorings(this ITextBuffer buffer)
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsRefactorings(buffer);

        /// <summary>
        /// Checks if a buffer supports rename.
        /// </summary>
        internal static bool SupportsRename(this ITextBuffer buffer)
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsRename(buffer);

        /// <summary>
        /// Checks if a buffer supports code fixes.
        /// </summary>
        internal static bool SupportsCodeFixes(this ITextBuffer buffer)
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsCodeFixes(buffer);

        /// <summary>
        /// Checks if a buffer supports navigation.
        /// </summary>
        internal static bool SupportsNavigationToAnyPosition(this ITextBuffer buffer)
            => TryGetSupportsFeatureService(buffer, out var service) && service.SupportsNavigationToAnyPosition(buffer);

        private static bool TryGetSupportsFeatureService(ITextBuffer buffer, out ITextBufferSupportsFeatureService service)
        {
            service = null;
            if (buffer.TryGetWorkspace(out var workspace))
            {
                service = workspace.Services.GetService<ITextBufferSupportsFeatureService>();
            }

            return service != null;
        }
    }
}
