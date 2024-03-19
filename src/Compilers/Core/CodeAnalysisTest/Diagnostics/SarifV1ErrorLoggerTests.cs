// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    // See also VB and C# command line unit tests for additional coverage.
    [Trait(Traits.Feature, Traits.Features.SarifErrorLogging)]
    public class SarifV1ErrorLoggerTests : SarifErrorLoggerTests
    {
        internal override SarifErrorLogger CreateLogger(
            Stream stream,
            string toolName,
            string toolFileVersion,
            Version toolAssemblyVersion,
            CultureInfo culture)
        {
            return new SarifV1ErrorLogger(stream, toolName, toolFileVersion, toolAssemblyVersion, culture);
        }

        protected override string ExpectedOutputForAdditionalLocationsAsRelatedLocations =>
@"{
  ""$schema"": ""http://json.schemastore.org/sarif-1.0.0"",
  ""version"": ""1.0.0"",
  ""runs"": [
    {
      ""tool"": {
        ""name"": ""toolName"",
        ""version"": ""1.2.3.4"",
        ""fileVersion"": ""1.2.3.4 for Windows"",
        ""semanticVersion"": ""1.2.3"",
        ""language"": ""fr-CA""
      },
      ""results"": [
        {
          ""ruleId"": ""TST"",
          ""level"": ""error"",
          ""locations"": [
            {
              ""resultFile"": {
                ""uri"": """ + (PathUtilities.IsUnixLikePlatform
                                    ? "Z:/Main%20Location.cs"
                                    : "file:///Z:/Main%20Location.cs") + @""",
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
                ""uri"": ""Relative%20Additional/Location.cs"",
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

        [Fact]
        public void AdditionalLocationsAsRelatedLocations()
        {
            AdditionalLocationsAsRelatedLocationsImpl();
        }

        protected override string ExpectedOutputForDescriptorIdCollision =>
@"{
  ""$schema"": ""http://json.schemastore.org/sarif-1.0.0"",
  ""version"": ""1.0.0"",
  ""runs"": [
    {
      ""tool"": {
        ""name"": ""toolName"",
        ""version"": ""1.2.3.4"",
        ""fileVersion"": ""1.2.3.4 for Windows"",
        ""semanticVersion"": ""1.2.3"",
        ""language"": ""en-US""
      },
      ""results"": [
        {
          ""ruleId"": ""TST001-001"",
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
          ""ruleKey"": ""TST001-002"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleKey"": ""TST001-003"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""level"": ""warning"",
          ""message"": ""messageFormat"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleKey"": ""TST002-001"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleKey"": ""TST002-002"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleKey"": ""TST002-003"",
          ""level"": ""error""
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleKey"": ""TST002-004"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleKey"": ""TST002-005"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001-001"",
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
          ""ruleKey"": ""TST001-002"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST001"",
          ""ruleKey"": ""TST001-003"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""level"": ""warning"",
          ""message"": ""messageFormat"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleKey"": ""TST002-001"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleKey"": ""TST002-002"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleKey"": ""TST002-003"",
          ""level"": ""error""
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleKey"": ""TST002-004"",
          ""level"": ""warning"",
          ""properties"": {
            ""warningLevel"": 1
          }
        },
        {
          ""ruleId"": ""TST002"",
          ""ruleKey"": ""TST002-005"",
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

        [Fact]
        public void DescriptorIdCollision()
        {
            DescriptorIdCollisionImpl();
        }

        [Fact]
        public void PathToUri()
        {
            PathToUriImpl(@"{{
  ""$schema"": ""http://json.schemastore.org/sarif-1.0.0"",
  ""version"": ""1.0.0"",
  ""runs"": [
    {{
      ""tool"": {{
        ""name"": """",
        ""version"": ""1.0.0"",
        ""fileVersion"": """",
        ""semanticVersion"": ""1.0.0""
      }},
      ""results"": [
        {{
          ""ruleId"": ""uriDiagnostic"",
          ""level"": ""warning"",
          ""message"": ""blank diagnostic"",
          ""locations"": [
            {{
              ""resultFile"": {{
                ""uri"": ""{0}"",
                ""region"": {{
                  ""startLine"": 1,
                  ""startColumn"": 1,
                  ""endLine"": 1,
                  ""endColumn"": 1
                }}
              }}
            }}
          ],
          ""properties"": {{
            ""warningLevel"": 3
          }}
        }}
      ],
      ""rules"": {{
        ""uriDiagnostic"": {{
          ""id"": ""uriDiagnostic"",
          ""defaultLevel"": ""warning"",
          ""properties"": {{
            ""isEnabledByDefault"": true
          }}
        }}
      }}
    }}
  ]
}}");
        }
    }
}
