// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SignTool.Json
{
    internal sealed class FileJson
    {
        [JsonProperty(PropertyName = "sign")]
        public FileSignData[] SignList { get; set; }

        [JsonProperty(PropertyName = "exclude")]
        public string[] ExcludeList { get; set; }

        public FileJson()
        {

        }
    }

    internal sealed class FileSignData
    {
        [JsonProperty(PropertyName = "certificate")]
        public string Certificate { get; set; }

        [JsonProperty(PropertyName = "strongName")]
        public string StrongName { get; set; }

        [JsonProperty(PropertyName = "values")]
        public string[] FileList { get; set; }

        public FileSignData()
        {
        }
    }
}
