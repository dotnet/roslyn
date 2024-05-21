// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal abstract class AbstractExportLspServiceAttribute : ExportAttribute
{
    /// <summary>
    /// The full assembly-qualified type name of the service being exported.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// The LSP server for which this service applies to.  If null, this service applies to any server
    /// with the matching contract name.
    /// </summary>
    public WellKnownLspServerKinds? ServerKind { get; }

    /// <summary>
    /// Services MEF exported as <see cref="ILspService"/> must by definition be stateless as they are
    /// shared amongst all LSP server instances through restarts.
    /// </summary>
    public bool IsStateless { get; }

    /// <summary>
    /// The full assembly-qualified type names of the interfaces the service implements.
    /// </summary>
    public string[] InterfaceNames { get; }

    private readonly Lazy<string?[]>? _lazyMethodHandlerData;

    /// <summary>
    /// If this this service implements <see cref="IMethodHandler"/>, returns a blob of binary data
    /// that encodes an array of <see cref="MethodHandlerDetails"/>s; otherwise <see langword="null"/>.
    /// </summary>
    public string?[]? MethodHandlerData => _lazyMethodHandlerData?.Value;

    protected AbstractExportLspServiceAttribute(
        Type serviceType, string contractName, Type contractType, bool isStateless, WellKnownLspServerKinds serverKind)
        : base(contractName, contractType)
    {
        Contract.ThrowIfFalse(serviceType.GetInterfaces().Contains(typeof(ILspService)), $"{serviceType.Name} does not inherit from {nameof(ILspService)}");
        Contract.ThrowIfNull(serviceType.AssemblyQualifiedName);

        TypeName = serviceType.AssemblyQualifiedName;
        IsStateless = isStateless;
        ServerKind = serverKind;

        InterfaceNames = Array.ConvertAll(serviceType.GetInterfaces(), t => t.AssemblyQualifiedName!);

        _lazyMethodHandlerData = typeof(IMethodHandler).IsAssignableFrom(serviceType)
            ? new(() => CreateMethodHandlerData(serviceType))
            : null;
    }

    private static string?[] CreateMethodHandlerData(Type handlerType)
    {
        var handlerDetails = MethodHandlerDetails.From(handlerType);

        var result = new string?[handlerDetails.Length * 5];

        var index = 0;
        foreach (var (methodName, language, requestTypeRef, responseTypeRef, requestContextTypeRef) in handlerDetails)
        {
            result[index++] = methodName;
            result[index++] = language;
            result[index++] = requestTypeRef?.TypeName;
            result[index++] = responseTypeRef?.TypeName;
            result[index++] = requestContextTypeRef.TypeName;
        }

        return result;
    }
}
