﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;
using System.IO;

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

        public ProvideRoslynBindingRedirectionAttribute(string fileName)
        {
            // ProvideBindingRedirectionAttribute is sealed, so we can't inherit from it to provide defaults.
            // Instead, we'll do more of an aggregation pattern here.
            // Note that PublicKeyToken, NewVersion and OldVersionUpperBound are read from the actual assembly version of the dll.
            _redirectionAttribute = new ProvideBindingRedirectionAttribute
            {
                AssemblyName = Path.GetFileNameWithoutExtension(fileName),
                OldVersionLowerBound = "0.0.0.0",
                CodeBase = fileName,
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
