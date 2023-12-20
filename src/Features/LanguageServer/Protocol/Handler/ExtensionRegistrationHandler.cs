// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(ExtensionRegistrationHandler)), Shared]
    [Method("extensions/registerExtension")]
    internal sealed class ExtensionRegistrationHandler : ILspServiceRequestHandler<ExtensionInfoAndMessage, string>
    {
        public bool MutatesSolutionState => true;

        public bool RequiresLSPSolution => true;

        public Task<string> HandleRequestAsync(ExtensionInfoAndMessage request, RequestContext context, CancellationToken cancellationToken)
        {
            var externalHandlers = LoadExternalAssemblies(request.AssemblyPath);

            var handlerProvider = context.GetRequiredService<IHandlerProvider>();
            handlerProvider.AddExternalExtensions(externalHandlers);

            return Task.FromResult("Hello from Roslyn. External handlers loaded: " + externalHandlers.Count);
        }

        private static ImmutableDictionary<RequestHandlerMetadata, Lazy<IMethodHandler>> LoadExternalAssemblies(string assemblyPath)
        {
            var externalAssembly = Assembly.LoadFrom(assemblyPath);

            var implementingClasses = externalAssembly.GetTypes()
                .Where(type => typeof(IExtensionMethodHandler).IsAssignableFrom(type) && !type.IsInterface);

            var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<RequestHandlerMetadata, Lazy<IMethodHandler>>();
            foreach (var ic in implementingClasses)
            {
                var methodName = implementingClasses.First().CustomAttributes.First(a => a.AttributeType.FullName == "Microsoft.CodeAnalysis.LanguageServer.Handler.MethodAttribute").ConstructorArguments.First().ToString();
                methodName = methodName.Replace("\"", "");

                // TODO: This is not robust
                var types = implementingClasses.First().BaseType.GenericTypeArguments;
                var requestType = types[0];
                var responseType = types[1];

                // Create the instance of the handler from the external extension
                var externalHandlerInstance = Activator.CreateInstance(ic);

                // Create the type of the internal wrapper 
                var wrapperType = typeof(ExtensionMethodHandlerWrapper<,,>).MakeGenericType(requestType, responseType, ic);
                var wrappedHandler = (IMethodHandler)Activator.CreateInstance(wrapperType, externalHandlerInstance);

                requestHandlerDictionary.Add(new RequestHandlerMetadata(methodName, requestType, responseType), new Lazy<IMethodHandler>(() => wrappedHandler));
            }

            return requestHandlerDictionary.ToImmutable();
        }
    }

    // TODO: This is temporary
    [DataContract]
    internal class ExtensionInfoAndMessage
    {
        [DataMember(Name = "assemblyPath")]
        public string AssemblyPath { get; set; }
        [DataMember(Name = "typeFullName")]
        public string TypeFullName { get; set; }
    }
}
