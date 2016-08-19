// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using Newtonsoft.Json.Linq;

namespace Roslyn.Test.Performance.Utilities.ConsumptionParser
{
    internal class RunInfo 
    {
        public string UserName { get; }
        public string Branch { get; }

        public RunInfo(string username, string branch)
        {
            this.UserName = username;
            this.Branch = branch;
        }

        public static RunInfo Parse(string s)
        {
            dynamic obj = JObject.Parse(s);
            string username = obj.roots[0].job.user.userName;
            string branch = obj.roots[0].job.jobGroup.jobGroupName;
            return new RunInfo(username, branch);
        }
    }
}
