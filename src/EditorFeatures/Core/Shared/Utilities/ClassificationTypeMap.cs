﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    [Export]
    internal class ClassificationTypeMap
    {
        private readonly Dictionary<string, IClassificationType> _identityMap;
        private readonly IClassificationTypeRegistryService _registryService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ClassificationTypeMap(
            IClassificationTypeRegistryService registryService)
        {
            _registryService = registryService;

            // Prepopulate the identity map with the constant string values from ClassificationTypeNames
            var fields = typeof(ClassificationTypeNames).GetFields();
            _identityMap = new Dictionary<string, IClassificationType>(fields.Length, ReferenceEqualityComparer.Instance);

            foreach (var field in fields)
            {
                // The strings returned from reflection do not have reference-identity
                // with the string constants used by the compiler. Fortunately, a call
                // to string.Intern fixes them.
                var rawValue = (string?)field.GetValue(null);
                Contract.ThrowIfNull(rawValue);
                var value = string.Intern(rawValue);
                _identityMap.Add(value, registryService.GetClassificationType(value));
            }
        }

        public IClassificationType GetClassificationType(string name)
        {
            var type = GetClassificationTypeWorker(name);
            if (type == null)
            {
                FatalError.ReportAndCatch(new Exception($"classification type doesn't exist for {name}"));
            }

            return type ?? GetClassificationTypeWorker(ClassificationTypeNames.Text);
        }

        private IClassificationType GetClassificationTypeWorker(string name)
        {
            return _identityMap.TryGetValue(name, out var result)
                ? result
                : _registryService.GetClassificationType(name);
        }
    }
}
