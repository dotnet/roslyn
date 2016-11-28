// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

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
                new[]
                {
                    new Lazy<IOptionSerializer, OptionSerializerMetadata>(
                    () =>
                    {
                        return new TestOptionSerializer();
                    },
                    new OptionSerializerMetadata(features))
                }), workspaceServices: null);
        }

        internal class TestOptionsProvider : IOptionProvider
        {
            public IEnumerable<IOption> GetOptions()
            {
                yield return new Option<bool>("Test Feature", "Test Name", false);
            }
        }

        internal class TestOptionSerializer : IOptionSerializer
        {
            public bool TryFetch(OptionKey optionKey, out object value)
            {
                value = null;
                return false;
            }

            public bool TryPersist(OptionKey optionKey, object value)
            {
                return false;
            }
        }
    }
}