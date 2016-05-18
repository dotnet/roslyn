// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using System.IO;
using System;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    // See also VB and C# command line unit tests for additional coverage.
    public class ErrorLoggerTests
    {
        [Fact]
        public void AdditionalLocationsAsRelatedLocations()
        {
            var stream = new MemoryStream();
            using (var logger = new ErrorLogger(stream, "toolName", "1.2.3.4", new Version(1, 2, 3, 4), CultureInfo.InvariantCulture))
            {
                var mainLocation = Location.Create(@"Z:\MainLocation.cs", new TextSpan(0, 0), new LinePositionSpan(LinePosition.Zero, LinePosition.Zero));
                var additionalLocation = Location.Create(@"Z:\AdditionalLocation.cs", new TextSpan(0, 0), new LinePositionSpan(LinePosition.Zero, LinePosition.Zero));
                var descriptor = new DiagnosticDescriptor("TST", "_TST_", "", "", DiagnosticSeverity.Error, false);

                IEnumerable<Location> additionalLocations = new[] { additionalLocation };
                logger.LogDiagnostic(Diagnostic.Create(descriptor, mainLocation, additionalLocations));
            }

            string expected = 
@"{
  ""$schema"": ""http://json.schemastore.org/sarif-1.0.0-beta.5"",
  ""version"": ""1.0.0-beta.5"",
  ""runs"": [
    {
      ""tool"": {
        ""name"": ""toolName"",
        ""version"": ""1.2.3.4"",
        ""fileVersion"": ""1.2.3.4"",
        ""semanticVersion"": ""1.2.3""
      },
      ""results"": [
        {
          ""ruleId"": ""TST"",
          ""level"": ""error"",
          ""locations"": [
            {
              ""resultFile"": {
                ""uri"": ""file:///Z:/MainLocation.cs"",
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
                ""uri"": ""file:///Z:/AdditionalLocation.cs"",
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
      ""rules"": {
        ""TST"": {
          ""id"": ""TST"",
          ""shortDescription"": ""_TST_"",
          ""defaultLevel"": ""error"",
          ""properties"": {
            ""isEnabledByDefault"": false
          }
        }
      }
    }
  ]
}";
            string actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DescriptorIdCollision()
        {
            var descriptors = new[] {
                new DiagnosticDescriptor("TST001.001",    "_TST001.001_",     "", "", DiagnosticSeverity.Warning, true),
                new DiagnosticDescriptor("TST001",        "_TST001_",         "", "", DiagnosticSeverity.Warning, true),
                new DiagnosticDescriptor("TST001",        "_TST001.002_",     "", "", DiagnosticSeverity.Warning, true),
                new DiagnosticDescriptor("TST001",        "_TST001.003_",     "", "", DiagnosticSeverity.Warning, true),
            };

            var stream = new MemoryStream();
            using (var logger = new ErrorLogger(stream, "toolName", "1.2.3.4", new Version(1, 2, 3, 4), CultureInfo.InvariantCulture))
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
  ""$schema"": ""http://json.schemastore.org/sarif-1.0.0-beta.5"",
  ""version"": ""1.0.0-beta.5"",
  ""runs"": [
    {
      ""tool"": {
        ""name"": ""toolName"",
        ""version"": ""1.2.3.4"",
        ""fileVersion"": ""1.2.3.4"",
        ""semanticVersion"": ""1.2.3""
      },
      ""results"": [
        {
          ""ruleId"": ""TST001.001"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleKey"": ""TST001.002"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleKey"": ""TST001.003"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001.001"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleKey"": ""TST001.002"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleKey"": ""TST001.003"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        }
      ],
      ""rules"": {
        ""TST001"": {
          ""id"": ""TST001"",
          ""shortDescription"": ""_TST001_"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""isEnabledByDefault"": true
          }
        },
        ""TST001.001"": {
          ""id"": ""TST001.001"",
          ""shortDescription"": ""_TST001.001_"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""isEnabledByDefault"": true
          }
        },
        ""TST001.002"": {
          ""id"": ""TST001"",
          ""shortDescription"": ""_TST001.002_"",
          ""defaultLevel"": ""warning"",
          ""properties"": {
            ""isEnabledByDefault"": true
          }
        },
        ""TST001.003"": {
          ""id"": ""TST001"",
          ""shortDescription"": ""_TST001.003_"",
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
            Assert.Equal(expected, actual);
        }
    }
}
