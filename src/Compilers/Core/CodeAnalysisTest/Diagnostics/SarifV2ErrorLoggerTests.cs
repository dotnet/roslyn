// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;

using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    // See also VB and C# command line unit tests for additional coverage.
    [Trait(Traits.Feature, Traits.Features.SarifErrorLogging)]
    public class SarifV2ErrorLoggerTests
    {
        [Fact]
        public void AdditionalLocationsAsRelatedLocations()
        {
            var stream = new MemoryStream();
            using (var logger = new SarifV2ErrorLogger(stream, "toolName", "1.2.3.4", new Version(1, 2, 3, 4), new CultureInfo("fr-CA", useUserOverride: false)))
            {
                var span = new TextSpan(0, 0);
                var position = new LinePositionSpan(LinePosition.Zero, LinePosition.Zero);
                var mainLocation = Location.Create(@"Z:\Main Location.cs", span, position);
                var descriptor = new DiagnosticDescriptor("TST", "_TST_", "", "", DiagnosticSeverity.Error, false);

                IEnumerable<Location> additionalLocations = new[] {
                    Location.Create(@"Relative Additional/Location.cs", span, position),
                    Location.Create(@"a:cannot/interpret/as\uri", span, position),
                };

                logger.LogDiagnostic(Diagnostic.Create(descriptor, mainLocation, additionalLocations));
            }

            string expected =
@"{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""results"": [
        {
          ""ruleId"": ""TST"",
          ""ruleIndex"": 0,
          ""level"": ""error"",
          ""locations"": [
            {
              ""physicalLocation"": {
                ""artifactLocation"": {
                  ""uri"": ""file:///Z:/Main%20Location.cs""
                },
                ""region"": {
                  ""startLine"": 1,
                  ""startColumn"": 1,
                  ""endLine"": 1,
                  ""endColumn"": 1
                }
              }
            }
          ],
          ""relatedLocations"": [
            {
              ""physicalLocation"": {
                ""artifactLocation"": {
                  ""uri"": ""Relative%20Additional/Location.cs""
                },
                ""region"": {
                  ""startLine"": 1,
                  ""startColumn"": 1,
                  ""endLine"": 1,
                  ""endColumn"": 1
                }
              }
            },
            {
              ""physicalLocation"": {
                ""artifactLocation"": {
                  ""uri"": ""a%3Acannot%2Finterpret%2Fas%5Curi""
                },
                ""region"": {
                  ""startLine"": 1,
                  ""startColumn"": 1,
                  ""endLine"": 1,
                  ""endColumn"": 1
                }
              }
            }
          ]
        }
      ],
      ""tool"": {
        ""driver"": {
          ""name"": ""toolName"",
          ""fileVersion"": ""1.2.3.4"",
          ""version"": ""1.2.3.4"",
          ""semanticVersion"": ""1.2.3"",
          ""language"": ""fr-CA"",
          ""rules"": [
            {
              ""id"": ""TST"",
              ""shortDescription"": {
                ""text"": ""_TST_""
              },
              ""defaultConfiguration"": {
                ""level"": ""error"",
                ""enabled"": false
              }
            }
          ]
        }
      }
    }
  ]
}";
            string actual = Encoding.UTF8.GetString(stream.ToArray());
            if (actual != expected)
            {
                File.WriteAllText(@"C:\Users\Larry Golding\Desktop\" + nameof(AdditionalLocationsAsRelatedLocations) + "-actual.sarif", actual);
                File.WriteAllText(@"C:\Users\Larry Golding\Desktop\" + nameof(AdditionalLocationsAsRelatedLocations) + "-expected.sarif", expected);
            }
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DescriptorIdCollision()
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
            using (var logger = new SarifV2ErrorLogger(stream, "toolName", "1.2.3.4", new Version(1, 2, 3, 4), new CultureInfo("en-US", useUserOverride: false)))
            {
                for (int i = 0; i < 2; i++)
                {
                    foreach (var descriptor in descriptors)
                    {
                        logger.LogDiagnostic(Diagnostic.Create(descriptor, Location.None));
                    }
                }
            }

            string expected =
@"{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""results"": [
        {
          ""ruleId"": ""TST001-001"",
          ""ruleIndex"": 0,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleIndex"": 1,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleIndex"": 2,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleIndex"": 3,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 4,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 4,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 4,
          ""level"": ""warning"",
          ""message"": {
            ""text"": ""messageFormat""
          },
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 5,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 6,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 7,
          ""level"": ""error""
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 8,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 9,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001-001"",
          ""ruleIndex"": 0,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleIndex"": 1,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleIndex"": 2,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleIndex"": 3,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 4,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 4,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 4,
          ""level"": ""warning"",
          ""message"": ""messageFormat"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 5,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 6,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 7,
          ""level"": ""error""
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 8,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleIndex"": 9,
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        }
      ],
      ""tool"": {
        ""driver"": {
          ""name"": ""toolName"",
          ""fileVersion"": ""1.2.3.4"",
          ""version"": ""1.2.3.4"",
          ""semanticVersion"": ""1.2.3"",
          ""rules"": [
            {
            ""id"": ""TST001"",
            ""shortDescription"": {
              ""text"": _TST001_""
            },
            ""defaultConfiguration"": {
              ""level"": ""warning"",
              ""enabled"": true
            }
          }
        },
        ""TST001-001"": {
          ""id"": ""TST001-001"",
          ""shortDescription"": ""_TST001-001_"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""isEnabledByDefault"": true
          }
        },
        ""TST001-002"": {
          ""id"": ""TST001"",
          ""shortDescription"": ""_TST001-002_"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""isEnabledByDefault"": true
          }
        },
        ""TST001-003"": {
          ""id"": ""TST001"",
          ""shortDescription"": ""_TST001-003_"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""isEnabledByDefault"": true
          }
        },
        ""TST002"": {
          ""id"": ""TST002"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""isEnabledByDefault"": true
          }
        },
        ""TST002-001"": {
          ""id"": ""TST002"",
          ""shortDescription"": ""title_001"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""isEnabledByDefault"": true
          }
        },
        ""TST002-002"": {
          ""id"": ""TST002"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""category"": ""category_002"",
            ""isEnabledByDefault"": true
          }
        },
        ""TST002-003"": {
          ""id"": ""TST002"",
          ""defaultLevel"": ""error"",
          ""properties"": {
            ""isEnabledByDefault"": true
          }
        },
        ""TST002-004"": {
          ""id"": ""TST002"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""isEnabledByDefault"": false
          }
        },
        ""TST002-005"": {
          ""id"": ""TST002"",
          ""fullDescription"": ""description_005"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""isEnabledByDefault"": true
          }
        }
      }
    }
  ]
}";
            string actual = Encoding.UTF8.GetString(stream.ToArray());
            if (actual != expected)
            {
                File.WriteAllText(@"C:\Users\Larry Golding\Desktop\" + nameof(DescriptorIdCollision) + "-actual.sarif", actual);
                File.WriteAllText(@"C:\Users\Larry Golding\Desktop\" + nameof(DescriptorIdCollision) + "-expected.sarif", expected);
            }
            Assert.Equal(expected, actual);
        }
    }
}
