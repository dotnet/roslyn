// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private sealed class DiagnosticAnalyzerComparer : IEqualityComparer<DiagnosticAnalyzer>
    {
        public static readonly DiagnosticAnalyzerComparer Instance = new();

        public bool Equals(DiagnosticAnalyzer? x, DiagnosticAnalyzer? y)
            => (x, y) switch
            {
                (null, null) => true,
                (null, _) => false,
                (_, null) => false,
                _ => GetAnalyzerIdAndLastWriteTime(x) == GetAnalyzerIdAndLastWriteTime(y)
            };

        public int GetHashCode(DiagnosticAnalyzer obj) => GetAnalyzerIdAndLastWriteTime(obj).GetHashCode();

        private static (string analyzerId, DateTime lastWriteTime) GetAnalyzerIdAndLastWriteTime(DiagnosticAnalyzer analyzer)
        {
            // Get the unique ID for given diagnostic analyzer.
            // note that we also put version stamp so that we can detect changed analyzer.
            var typeInfo = analyzer.GetType().GetTypeInfo();
            return (analyzer.GetAnalyzerId(), GetAnalyzerLastWriteTime(typeInfo.Assembly.Location));
        }

        private static DateTime GetAnalyzerLastWriteTime(string path)
        {
            if (path == null || !File.Exists(path))
                return default;

            return File.GetLastWriteTimeUtc(path);
        }
    }
}
