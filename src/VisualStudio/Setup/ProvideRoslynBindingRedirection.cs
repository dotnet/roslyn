// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;

namespace Roslyn.VisualStudio.Setup
{
    /// <summary>
    /// A <see cref="RegistrationAttribute"/> that provides binding redirects with all of the Roslyn settings we need.
    /// It's just a wrapper for <see cref="ProvideBindingRedirectionAttribute"/> that sets all the defaults rather than duplicating them.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class ProvideRoslynBindingRedirectionAttribute : RegistrationAttribute
    {
        private readonly ProvideBindingRedirectionAttribute _redirectionAttribute;

#if OFFICIAL_BUILD

        // We should not include CodeBase attributes because we want them to get loaded from PrivateAssemblies
        public const bool GenerateCodeBase = false;

#else
        // We should include CodeBase attributes so they always are loaded from this extension
        public const bool GenerateCodeBase = true;

#endif

        public ProvideRoslynBindingRedirectionAttribute(string assemblyName)
        {
            // ProvideBindingRedirectionAttribute is sealed, so we can't inherit from it to provide defaults.
            // Instead, we'll do more of an aggregation pattern here.
            _redirectionAttribute = new ProvideBindingRedirectionAttribute
            {
                AssemblyName = assemblyName,
                PublicKeyToken = "31BF3856AD364E35",
                OldVersionLowerBound = "0.7.0.0",
                OldVersionUpperBound = "1.1.0.0",

#if OFFICIAL_BUILD
                // If this is an official build we want to generate binding
                // redirects from our old versions to the release version 
                NewVersion = "1.1.0.0",
#else
                // Non-official builds get redirects to local 42.42.42.42,
                // which will only be built locally
                NewVersion = "42.42.42.42",
#endif

                GenerateCodeBase = GenerateCodeBase,
            };
        }

        public override void Register(RegistrationContext context)
        {
            _redirectionAttribute.Register(context);

            // Opt into overriding the devenv.exe.config binding redirect
            using (var key = context.CreateKey(@"RuntimeConfiguration\dependentAssembly\bindingRedirection\" + _redirectionAttribute.Guid.ToString("B").ToUpperInvariant()))
            {
                key.SetValue("isPkgDefOverrideEnabled", true);
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            _redirectionAttribute.Unregister(context);
        }

    }
}
