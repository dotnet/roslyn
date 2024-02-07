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

[ExportLanguageService(typeof(IMapCodeService), language: LanguageNames.CSharp), Shared]
internal sealed class CSharpMapCodeService : IMapCodeService
{
    private const string CodeMapperDllName = "Microsoft.VisualStudio.Conversations.CodeMappers.CSharp, Version=0.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    private const string MapCodeServiceTypeFullName = "Microsoft.VisualStudio.Conversations.CodeMappers.CSharp.CSharpMapCodeService";
    private const string MapCodeAsyncMethodName = "MapCodeAsync";

    private readonly ICSharpCopilotMapCodeService? _service;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpMapCodeService([Import(AllowDefault = true)] ICSharpCopilotMapCodeService? service)
    {
        _service = service;
    }

    public Task<ImmutableArray<TextChange>?> MapCodeAsync(Document document, ImmutableArray<string> contents, ImmutableArray<(Document, TextSpan)> focusLocations, CancellationToken cancellationToken)
    {
        var options = new Dictionary<string, object>();
        if (_service is not null)
        {
            return _service.MapCodeAsync(document, contents, focusLocations, options, cancellationToken);
        }

        return TryLoadAndInvokeViaReflectionAsync();

        // The implementation of ICSharpCopilotMapCodeService is in Copilot Chat repo, which can't reference the EA package
        // since it's shipped as a separate vsix and needs to maintain compatibility for older VS versions. So we try to call
        // the service via reflection here until they can move to newer Roslyn version.
        // https://github.com/dotnet/roslyn/issues/69967
        Task<ImmutableArray<TextChange>?> TryLoadAndInvokeViaReflectionAsync()
        {
            try
            {
                var assembly = Assembly.Load(CodeMapperDllName);
                var type = assembly.GetType(MapCodeServiceTypeFullName);
                if (type?.GetMethod(MapCodeAsyncMethodName, BindingFlags.Instance | BindingFlags.Public) is MethodInfo method)
                {
                    var instance = Activator.CreateInstance(type);
                    return (Task<ImmutableArray<TextChange>?>)method.Invoke(instance, [document, contents, focusLocations, options, cancellationToken])!;
                }
            }
            catch
            {
                // Catch all here since failure is expected if user has no copilot chat or an older version of it installed.
            }

            return Task.FromResult<ImmutableArray<TextChange>?>(null);
        }
    }

}
