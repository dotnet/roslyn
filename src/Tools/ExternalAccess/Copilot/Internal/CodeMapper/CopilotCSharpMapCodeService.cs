// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.CodeMapper;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MapCode;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.CodeMapper;

using MapCodeAsyncDelegateType = Func<Document, ImmutableArray<string>, ImmutableArray<(Document Document, TextSpan TextSpan)>, Dictionary<string, object>, CancellationToken, Task<ImmutableArray<TextChange>?>>;

[ExportLanguageService(typeof(IMapCodeService), language: LanguageNames.CSharp), Shared]
internal sealed class CSharpMapCodeService : IMapCodeService
{
    private readonly ICSharpCopilotMapCodeService _service;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpMapCodeService([Import(AllowDefault = true)] ICSharpCopilotMapCodeService? service)
    {
        _service = service ?? new ReflectionWrapper();
    }

    public Task<ImmutableArray<TextChange>?> MapCodeAsync(Document document, ImmutableArray<string> contents, ImmutableArray<(Document, TextSpan)> focusLocations, CancellationToken cancellationToken)
    {
        var options = new Dictionary<string, object>();
        return _service.MapCodeAsync(document, contents, focusLocations, options, cancellationToken);
    }

    private sealed class ReflectionWrapper : ICSharpCopilotMapCodeService
    {
        private const string CodeMapperDllName = "Microsoft.VisualStudio.Copilot.CodeMappers.CSharp, Version=0.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        private const string MapCodeServiceTypeFullName = "Microsoft.VisualStudio.Conversations.CodeMappers.CSharp.CSharpMapCodeService";
        private const string MapCodeAsyncMethodName = "MapCodeAsync";

        // Create and cache the delegate to ensure we use a singleton and better performance.
        private readonly Lazy<MapCodeAsyncDelegateType?> _lazyMapCodeAsyncDelegate = new(CreateDelegate, LazyThreadSafetyMode.PublicationOnly);

        private static MapCodeAsyncDelegateType? CreateDelegate()
        {
            try
            {
                var assembly = Assembly.Load(CodeMapperDllName);
                var type = assembly.GetType(MapCodeServiceTypeFullName);
                if (type is null)
                    return null;

                var serviceInstance = Activator.CreateInstance(type);
                if (serviceInstance is null)
                    return null;

                if (type.GetMethod(MapCodeAsyncMethodName, [typeof(Document), typeof(ImmutableArray<string>), typeof(ImmutableArray<(Document Document, TextSpan TextSpan)>), typeof(Dictionary<string, object>), typeof(CancellationToken)]) is not MethodInfo mapCodeAsyncMethod)
                    return null;

                return (MapCodeAsyncDelegateType)Delegate.CreateDelegate(typeof(MapCodeAsyncDelegateType), serviceInstance, mapCodeAsyncMethod);

            }
            catch
            {
                // Catch all here since failure is expected if user has no copilot chat or an older version of it installed.
            }

            return null;
        }

        public async Task<ImmutableArray<TextChange>?> MapCodeAsync(Document document, ImmutableArray<string> contents, ImmutableArray<(Document document, TextSpan textSpan)> prioritizedFocusLocations, Dictionary<string, object> options, CancellationToken cancellationToken)
        {
            if (_lazyMapCodeAsyncDelegate.Value is null)
                return null;

            return await _lazyMapCodeAsyncDelegate.Value(document, contents, prioritizedFocusLocations, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
