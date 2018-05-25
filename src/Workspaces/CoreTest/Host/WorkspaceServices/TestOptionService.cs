// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal static class TestOptionService
    {
        public static OptionServiceFactory.OptionService GetService()
        {
            var features = new Dictionary<string, object>();
            features.Add("Features", new List<string>(new[] { "Test Features" }));
            return new OptionServiceFactory.OptionService(new GlobalOptionService(new[]
                {
                    new Lazy<IOptionProvider>(() => new TestOptionsProvider())
                },
                Enumerable.Empty<Lazy<IOptionPersister>>()), workspaceServices: new AdhocWorkspace().Services);
        }

        internal class TestOptionsProvider : IOptionProvider
        {
            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                new Option<bool>("Test Feature", "Test Name", false));
        }
    }
}
