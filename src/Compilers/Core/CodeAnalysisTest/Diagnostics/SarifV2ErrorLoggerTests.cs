// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    // See also VB and C# command line unit tests for additional coverage.
    [Trait(Traits.Feature, Traits.Features.SarifErrorLogging)]
    public class SarifV2ErrorLoggerTests : SarifErrorLoggerTests
    {
        internal override SarifErrorLogger CreateLogger(
            Stream stream,
            string toolName,
            string toolFileVersion,
            Version toolAssemblyVersion,
            CultureInfo culture)
        {
            return new SarifV2ErrorLogger(stream, toolName, toolFileVersion, toolAssemblyVersion, culture);
        }

        protected override string ExpectedOutputForAdditionalLocationsAsRelatedLocations =>
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
          ""version"": ""1.2.3.4 for Windows"",
          ""dottedQuadFileVersion"": ""1.2.3.4"",
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
      },
      ""columnKind"": ""utf16CodeUnits""
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
        }
      ],
      ""tool"": {
        ""driver"": {
          ""name"": ""toolName"",
          ""version"": ""1.2.3.4 for Windows"",
          ""dottedQuadFileVersion"": ""1.2.3.4"",
          ""semanticVersion"": ""1.2.3"",
          ""language"": ""en-US"",
          ""rules"": [
            {
              ""id"": ""TST001-001"",
              ""shortDescription"": {
                ""text"": ""_TST001-001_""
              }
            },
            {
              ""id"": ""TST001"",
              ""shortDescription"": {
                ""text"": ""_TST001_""
              }
            },
            {
              ""id"": ""TST001"",
              ""shortDescription"": {
                ""text"": ""_TST001-002_""
              }
            },
            {
              ""id"": ""TST001"",
              ""shortDescription"": {
                ""text"": ""_TST001-003_""
              }
            },
            {
              ""id"": ""TST002""
            },
            {
              ""id"": ""TST002"",
              ""shortDescription"": {
                ""text"": ""title_001""
              }
            },
            {
              ""id"": ""TST002"",
              ""properties"": {
                ""category"": ""category_002""
              }
            },
            {
              ""id"": ""TST002"",
              ""defaultConfiguration"": {
                ""level"": ""error""
              }
            },
            {
              ""id"": ""TST002"",
              ""defaultConfiguration"": {
                ""enabled"": false
              }
            },
            {
              ""id"": ""TST002"",
              ""fullDescription"": {
                ""text"": ""description_005""
              }
            }
          ]
        }
      },
      ""columnKind"": ""utf16CodeUnits""
    }
  ]
}";

        [Fact]
        public void DescriptorIdCollision()
        {
            DescriptorIdCollisionImpl();
        }
    }
}
