// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                var value = string.Intern((string)field.GetValue(null));
                _identityMap.Add(value, registryService.GetClassificationType(value));
            }
        }

        public IClassificationType GetClassificationType(string name)
        {
            var type = GetClassificationTypeWorker(name);
            if (type == null)
            {
                FatalError.ReportWithoutCrash(new Exception($"classification type doesn't exist for {name}"));
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
