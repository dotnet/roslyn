using System;
using Newtonsoft.Json.Linq;

namespace Roslyn.Test.Performance.Utilities.ConsumptionParser
{

    internal class RunInfo 
    {
        public string Username{ get; }
        public string Branch { get; }

        public RunInfo(string username, string branch)
        {
            this.Username = username;
            this.Branch = branch;
        }

        public static RunInfo Parse(String s)
        {
            dynamic obj = JObject.Parse(s);
            String username = obj.roots[0].job.user.userName;
            String branch = obj.roots[0].job.jobGroup.jobGroupName;
            return new RunInfo(username, branch);
        }
    }
}
