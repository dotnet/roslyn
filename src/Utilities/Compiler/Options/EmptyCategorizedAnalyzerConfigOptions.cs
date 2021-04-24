// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    internal sealed class EmptyCategorizedAnalyzerConfigOptions : ICategorizedAnalyzerConfigOptions
    {
        public static readonly EmptyCategorizedAnalyzerConfigOptions Empty = new();

        private EmptyCategorizedAnalyzerConfigOptions()
        {
        }

        public bool IsEmpty => true;

        public T GetOptionValue<T>(string optionName, SyntaxTree? tree, DiagnosticDescriptor? rule, CategorizedAnalyzerConfigOptionsExtensions.TryParseValue<T> tryParseValue, T defaultValue, OptionKind kind = OptionKind.DotnetCodeQuality)
        {
            return defaultValue;
        }
    }
}
