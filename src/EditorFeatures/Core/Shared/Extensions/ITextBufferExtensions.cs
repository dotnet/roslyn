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
        public static IEnumerable<DocumentId> GetOpenDocumentIds(this ITextBuffer buffer)
        {
            var container = buffer.AsTextContainer();

            Workspace workspace;
            if (Workspace.TryGetWorkspace(container, out workspace))
            {
                return workspace.GetRelatedDocumentIds(container);
            }

            return SpecializedCollections.EmptyEnumerable<DocumentId>();
        }

        internal static OptionSet TryGetOptions(this ITextBuffer textBuffer)
        {
            Workspace workspace;

            if (Workspace.TryGetWorkspace(textBuffer.AsTextContainer(), out workspace))
            {
                var service = workspace.Services.GetService<IOptionService>();

                if (service != null)
                {
                    return service.GetOptions();
                }
            }

            return null;
        }

        internal static T GetOption<T>(this ITextBuffer buffer, Option<T> option)
        {
            var options = TryGetOptions(buffer);

            if (options != null)
            {
                return options.GetOption(option);
            }

            return option.DefaultValue;
        }

        internal static T GetOption<T>(this ITextBuffer buffer, PerLanguageOption<T> option)
        {
            // Add a FailFast to help diagnose 984249.  Hopefully this will let us know what the issue is.
            try
            {
                Workspace workspace;
                if (!Workspace.TryGetWorkspace(buffer.AsTextContainer(), out workspace))
                {
                    return option.DefaultValue;
                }

                var service = workspace.Services.GetService<IOptionService>();
                if (service == null)
                {
                    return option.DefaultValue;
                }

                var language = workspace.Services.GetLanguageServices(buffer).Language;
                return service.GetOption(option, language);
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
