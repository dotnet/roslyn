// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal static class ExtractMethodService
    {
        public static Task<ExtractMethodResult> ExtractMethodAsync(Document document, TextSpan textSpan, bool extractLocalMethod = false, bool preferStatic = true, OptionSet options = null, CancellationToken cancellationToken = default)
        {
            return document.GetLanguageService<IExtractMethodService>().ExtractMethodAsync(document, textSpan, extractLocalMethod, preferStatic, options, cancellationToken);
        }
    }
}
