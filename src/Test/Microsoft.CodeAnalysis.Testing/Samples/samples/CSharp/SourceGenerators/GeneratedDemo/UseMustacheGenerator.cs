using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mustache;
using static System.Console;
using static GeneratedDemo.UseMustacheGenerator;

[assembly: Mustache("Lottery", t1, h1)]
[assembly: Mustache("HR", t2, h2)]
[assembly: Mustache("HTML", t3, h3)]
[assembly: Mustache("Section", t4, h4)]
[assembly: Mustache("NestedSection", t5, h5)]

namespace GeneratedDemo
{
    class UseMustacheGenerator
    {
        public static void Run()
        {
            WriteLine(Mustache.Constants.Lottery);
            WriteLine(Mustache.Constants.HR);
            WriteLine(Mustache.Constants.HTML);
            WriteLine(Mustache.Constants.Section);
            WriteLine(Mustache.Constants.NestedSection);
        }

        // Mustache templates and hashes from the manual at https://mustache.github.io/mustache.1.html...
        public const string t1 = @"
Hello {{name}}
You have just won {{value}} dollars!
{{#in_ca}}
Well, {{taxed_value}} dollars, after taxes.
{{/in_ca}}
";
        public const string h1 = @"
{
  ""name"": ""Chris"",
  ""value"": 10000,
  ""taxed_value"": 5000,
  ""in_ca"": true
}
";
        public const string t2 = @"
* {{name}}
* {{age}}
* {{company}}
* {{{company}}}
";
        public const string h2 = @"
{
  ""name"": ""Chris"",
  ""company"": ""<b>GitHub</b>""
}
";
        public const string t3 = @"
Shown
{{#person}}
  Never shown!
{{/person}}
";
        public const string h3 = @"
{
  ""person"": false
}
";
        public const string t4 = @"
{{#repo}}
  <b>{{name}}</b>
{{/repo}}
";
        public const string h4 = @"
{
  ""repo"": [
    { ""name"": ""resque"" },
    { ""name"": ""hub"" },
    { ""name"": ""rip"" }
  ]
}
";
        public const string t5 = @"
{{#repo}}
  <b>{{name}}</b>
    {{#nested}}
        NestedName: {{name}}
    {{/nested}}
{{/repo}}
";
        public const string h5 = @"
{
  ""repo"": [
    { ""name"": ""resque"", ""nested"":[{""name"":""nestedResque""}] },
    { ""name"": ""hub"" },
    { ""name"": ""rip"" }
  ]
}
";

    }
}
