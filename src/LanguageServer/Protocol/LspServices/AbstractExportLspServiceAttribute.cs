// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal abstract class AbstractExportLspServiceAttribute : ExportAttribute
{
    /// <summary>
    /// The fully-qualified type name of the service being exported.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// The full name of the assembly containing the service.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// The code base of the assembly, if any.
    /// </summary>
    public string? CodeBase { get; }

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

        Contract.ThrowIfNull(serviceType.FullName);
        TypeName = serviceType.FullName;

        Contract.ThrowIfNull(serviceType.Assembly.FullName);
        AssemblyName = serviceType.Assembly.FullName;

#pragma warning disable SYSLIB0012 // Type or member is obsolete
        CodeBase = serviceType.Assembly.CodeBase;
#pragma warning restore SYSLIB0012 // Type or member is obsolete

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

        using var _ = ArrayBuilder<string?>.GetInstance(out var result);

        foreach (var (methodName, language, requestTypeRef, responseTypeRef, requestContextTypeRef) in handlerDetails)
        {
            result.Add(methodName);
            result.Add(language);
            AddTypeRef(requestTypeRef);
            AddTypeRef(responseTypeRef);
            AddTypeRef(requestContextTypeRef);
        }

        return result.ToArray();

        void AddTypeRef(TypeRef? typeRef)
        {
            if (typeRef is TypeRef t)
            {
                result.Add(t.TypeName);
                result.Add(t.AssemblyName);
                result.Add(t.CodeBase);
            }
            else
            {
                result.Add(null);
            }
        }
    }
}
