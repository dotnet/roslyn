﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public abstract class SarifErrorLoggerTests
    {
        internal abstract SarifErrorLogger CreateLogger(
            Stream stream,
            string toolName,
            string toolFileVersion,
            Version toolAssemblyVersion,
            CultureInfo culture);

        protected abstract string ExpectedOutputForAdditionalLocationsAsRelatedLocations { get; }
        protected abstract string ExpectedOutputForDescriptorIdCollision { get; }

        public void AdditionalLocationsAsRelatedLocationsImpl()
        {
            var stream = new MemoryStream();
            using (var logger = CreateLogger(stream, "toolName", "1.2.3.4 for Windows", new Version(1, 2, 3, 4), new CultureInfo("fr-CA", useUserOverride: false)))
            {
                var span = new TextSpan(0, 0);
                var position = new LinePositionSpan(LinePosition.Zero, LinePosition.Zero);
                var mainLocation = Location.Create(@"Z:\Main Location.cs", span, position);
                var descriptor = new DiagnosticDescriptor("TST", "_TST_", "", "", DiagnosticSeverity.Error, false);

                IEnumerable<Location> additionalLocations = new[] {
                    Location.Create(@"Relative Additional/Location.cs", span, position),
                };

                logger.LogDiagnostic(Diagnostic.Create(descriptor, mainLocation, additionalLocations));
            }

            string actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(ExpectedOutputForAdditionalLocationsAsRelatedLocations, actual);
        }

        public void DescriptorIdCollisionImpl()
        {
            var descriptors = new[] {
                // Toughest case: generation of TST001-001 collides with with actual TST001-001 and must be bumped to TST001-002
                new DiagnosticDescriptor("TST001-001",    "_TST001-001_",     "", "", DiagnosticSeverity.Warning, true),
                new DiagnosticDescriptor("TST001",        "_TST001_",         "", "", DiagnosticSeverity.Warning, true),
                new DiagnosticDescriptor("TST001",        "_TST001-002_",     "", "", DiagnosticSeverity.Warning, true),
                new DiagnosticDescriptor("TST001",        "_TST001-003_",     "", "", DiagnosticSeverity.Warning, true),

                // Descriptors with same values should not get distinct entries in log
                new DiagnosticDescriptor("TST002", "", "", "", DiagnosticSeverity.Warning, true),
                new DiagnosticDescriptor("TST002", "", "", "", DiagnosticSeverity.Warning, true),

                // Changing only the message format (which we do not write out) should not produce a distinct entry in log.
                new DiagnosticDescriptor("TST002", "", "messageFormat", "", DiagnosticSeverity.Warning, true),

                // Changing any property that we do write out should create a distinct entry
                new DiagnosticDescriptor("TST002", "title_001", "", "", DiagnosticSeverity.Warning, true),
                new DiagnosticDescriptor("TST002", "", "", "category_002", DiagnosticSeverity.Warning, true),
                new DiagnosticDescriptor("TST002", "", "", "", DiagnosticSeverity.Error /*003*/, true),
                new DiagnosticDescriptor("TST002", "", "", "", DiagnosticSeverity.Warning, isEnabledByDefault: false /*004*/),
                new DiagnosticDescriptor("TST002", "", "", "", DiagnosticSeverity.Warning, true, "description_005"),
            };

            var stream = new MemoryStream();
            using (var logger = CreateLogger(stream, "toolName", "1.2.3.4 for Windows", new Version(1, 2, 3, 4), new CultureInfo("en-US", useUserOverride: false)))
            {
                for (int i = 0; i < 2; i++)
                {
                    foreach (var descriptor in descriptors)
                    {
                        logger.LogDiagnostic(Diagnostic.Create(descriptor, Location.None));
                    }
                }
            }

            string actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(ExpectedOutputForDescriptorIdCollision, actual);
        }

        protected void PathToUriImpl(string formatString)
        {
            var isUnix = PathUtilities.IsUnixLikePlatform;
            var paths = new[] {
                (@"A:\B\C\\..\D.cs", isUnix ? @"A:/B/C/D.cs" : "file:///A:/B/D.cs"),
                (@"A\B\C\\..\D.cs", isUnix ? @"A%5CB%5CC%5C%5C..%5CD.cs" : @"A/B/D.cs")
            };

            foreach (var (inputPath, outputPath) in paths)
            {
                var stream = new MemoryStream();

                using (var logger = CreateLogger(
                    stream,
                    toolName: "",
                    toolFileVersion: "",
                    toolAssemblyVersion: Version.Parse("1.0.0"),
                    CultureInfo.InvariantCulture))
                {
                    var location = Location.Create(
                        inputPath,
                        textSpan: default,
                        lineSpan: default);

                    logger.LogDiagnostic(Diagnostic.Create(
                        "uriDiagnostic",
                        category: "",
                        message: "blank diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true,
                        warningLevel: 3,
                        location: location));
                }

                var buffer = stream.ToArray();
                Assert.Equal(
                    string.Format(formatString, outputPath),
                    Encoding.UTF8.GetString(buffer, 0, buffer.Length),
                    ignoreLineEndingDifferences: true,
                    ignoreWhiteSpaceDifferences: true);
            }
        }
    }
}
