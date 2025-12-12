// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting;

internal interface IUnitTestingCodeLensContext
{
    Task<ImmutableArray<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(
        Guid projectGuid, string filePath, TextSpan span, DocumentId? sourceGeneratedDocumentId, CancellationToken cancellationToken);
}

internal interface IUnitTestingFeaturesReferencesServiceCallback
{
    Task<TResult> InvokeAsync<TResult>(string targetName, IReadOnlyList<object?> arguments, CancellationToken cancellationToken);
}

internal static class UnitTestingFeaturesReferencesService
{
    public static DocumentId? GetSourceGeneratorDocumentId(IDictionary<object, object> descriptorProperties)
        => CodeLensHelpers.GetSourceGeneratorDocumentId(descriptorProperties);

    public static async Task<ImmutableArray<(string MethodFullyQualifedName, string MethodOutputFilePath)>> GetCallerMethodsAsync(
        Guid projectGuid,
        string filePath,
        TextSpan span,
        DocumentId? sourceGeneratedDocumentId,
        IUnitTestingFeaturesReferencesServiceCallback callback,
        CancellationToken cancellationToken)
    {
        var callerMethods = await callback.InvokeAsync<ImmutableArray<ReferenceMethodDescriptor>?>(
            nameof(IUnitTestingCodeLensContext.FindReferenceMethodsAsync),
            [projectGuid, filePath, span, sourceGeneratedDocumentId],
            cancellationToken).ConfigureAwait(false);

        if (!callerMethods.HasValue || callerMethods.Value.IsEmpty)
        {
            return [];
        }

        return callerMethods.Value.SelectAsArray(m => (
            MethodFullyQualifiedName: m.FullName,
            MethodOutputFilePath: m.OutputFilePath));
    }
}
