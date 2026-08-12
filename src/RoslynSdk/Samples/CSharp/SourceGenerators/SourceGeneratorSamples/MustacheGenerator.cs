using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#nullable enable
#pragma warning disable RS1035 // Do not use banned APIs for analyzers

namespace Mustache
{
    [Generator]
    public class MustacheGenerator : ISourceGenerator
    {
        private const string attributeSource = @"
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)]
    internal sealed class MustacheAttribute: System.Attribute
    {
        public string Name { get; }
        public string Template { get; }
        public string Hash { get; }
        public MustacheAttribute(string name, string template, string hash)
            => (Name, Template, Hash) = (name, template, hash);
    }
";

        public void Execute(GeneratorExecutionContext context)
        {
            SyntaxReceiver rx = (SyntaxReceiver)context.SyntaxContextReceiver!;
            foreach ((string name, string template, string hash) in rx.TemplateInfo)
            {
                string source = SourceFileFromMustachePath(name, template, hash);
                context.AddSource($"Mustache{name}.g.cs", source);
            }
        }

        static string SourceFileFromMustachePath(string name, string template, string hash)
        {
            Func<object, string> tree = HandlebarsDotNet.Handlebars.Compile(template);
            object? @object = Newtonsoft.Json.JsonConvert.DeserializeObject(hash);
            if (@object is null)
            {
                return string.Empty;
            }

            string mustacheText = tree(@object);

            StringBuilder sb = new StringBuilder();
            sb.Append($@"
namespace Mustache {{

    public static partial class Constants {{

        public const string {name} = @""{mustacheText.Replace("\"", "\"\"")}"";
    }}
}}
");
            return sb.ToString();
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((pi) => pi.AddSource("Mustache_MainAttributes__.g.cs", attributeSource));
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<(string name, string template, string hash)> TemplateInfo = new List<(string name, string template, string hash)>();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                // find all valid mustache attributes
                if (context.Node is AttributeSyntax attrib
                    && attrib.ArgumentList?.Arguments.Count == 3
                    && context.SemanticModel.GetTypeInfo(attrib).Type?.ToDisplayString() == "MustacheAttribute")
                {
                    string name = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments[0].Expression).ToString();
                    string template = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments[1].Expression).ToString();
                    string hash = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments[2].Expression).ToString();

                    TemplateInfo.Add((name, template, hash));
                }
            }
        }
    }
}
