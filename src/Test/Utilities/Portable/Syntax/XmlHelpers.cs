// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class XmlOptions
    {
        public bool Trivia
        {
            get;
            set;
        }

        public bool Spans
        {
            get;
            set;
        }

        public bool ReflectionInfo
        {
            get;
            set;
        }

        public bool Text
        {
            get;
            set;
        }

        public bool Errors
        {
            get;
            set;
        }
    }

    public static class XmlHelpers
    {
        private static void AddNodeInfo(NodeInfo info, XElement xml)
        {
            xml.Add(new XAttribute("Type", info.ClassName));
            foreach (var f in info.FieldInfos)
            {
                if (!(f.PropertyName.Contains("Span") || f.PropertyName.Contains("Kind") || f.PropertyName.Contains("Text") || f.PropertyName.Contains("IsMissing")))
                {
                    xml.Add(@"<<%= f.PropertyName %> FieldType=<%= f.FieldType.Name %>><%= New XCData(f.Value.ToString) %></>");
                }
            }
        }

        public static void AddInfo(SyntaxNode node, XElement xml, XmlOptions options)
        {
            AddNodeInfo(node.GetNodeInfo(), xml);
        }

        public static void AddInfo(SyntaxNodeOrToken node, XElement xml, XmlOptions options)
        {
            AddNodeInfo(node.GetNodeInfo(), xml);
        }

        public static void AddInfo(SyntaxToken node, XElement xml, XmlOptions options)
        {
            AddNodeInfo(node.GetNodeInfo(), xml);
        }

        public static void AddInfo(SyntaxTrivia node, XElement xml, XmlOptions options)
        {
            AddNodeInfo(node.GetNodeInfo(), xml);
        }

        public static void AddErrors(XElement xml, IEnumerable<Diagnostic> errors, XmlOptions options)
        {
            xml.Add(@"<Errors>
                    <%= From e In errors
                        Let l = e.Location
                        Select If(l.InSource,
                        <Error Code=<%= e.Info.MessageIdentifier.ToString %> Severity=<%= e.Severity.ToString %>>
                            <%= If(options.Text, <Message><%= e.GetMessage(Globalization.CultureInfo.CurrentCulture) %></Message>, Nothing) %>
                            <%= If(options.Spans, <Span Start=<%= l.SourceSpan.Start %> End=<%= l.SourceSpan.End %> Length=<%= l.SourceSpan.Length %>/>, Nothing) %>
                        </Error>,
                        <Error Code=<%= e.Info.MessageIdentifier.ToString %> Severity=<%= e.Severity.ToString %>>
                            <%= If(options.Text, <Message><%= e.GetMessage(Globalization.CultureInfo.CurrentCulture) %></Message>, Nothing) %>
                        </Error>
                        ) %>
                </Errors>");
        }

        public static XElement ToXml(this SyntaxNodeOrToken node, SyntaxTree syntaxTree, XmlOptions options = null)
        {
            XElement xml = null;
            if (node.IsNode)
            {
                xml = ToXml(node.AsNode(), syntaxTree, options);
            }
            else
            {
                xml = ToXml(node.AsToken(), syntaxTree, options);
            }

            return xml;
        }

        public static XElement ToXml(this SyntaxNode node, SyntaxTree syntaxTree, XmlOptions options = null)
        {
            if (options == null)
            {
                options = new XmlOptions();
            }

            XElement xml = null;
            if (node != null)
            {
                xml = new XElement("Node",
                new XAttribute("IsToken", false),
                new XAttribute("IsTrivia", true),
                new XAttribute("Kind", node.GetKind()),
                new XAttribute("IsMissing", node.IsMissing));

                if (options.Spans)
                {
                    xml.Add(@"<Span Start=<%= node.SpanStart %> End=<%= node.Span.End %> Length=<%= node.Span.Length %>/>");
                    xml.Add(@"<FullSpan Start=<%= node.FullSpan.Start %> End=<%= node.FullSpan.End %> Length=<%= node.FullSpan.Length %>/>");
                }

                if (options.ReflectionInfo)
                {
                    AddInfo(node, xml, options);
                }

                if (options.Errors)
                {
                    if (syntaxTree.GetDiagnostics(node).Any())
                    {
                        AddErrors(xml, syntaxTree.GetDiagnostics(node), options);
                    }
                }

                foreach (var c in node.ChildNodesAndTokens())
                {
                    xml.Add(c.ToXml(syntaxTree, options));
                }
            }

            return xml;
        }

        public static XElement ToXml(this SyntaxToken token, SyntaxTree syntaxTree, XmlOptions options = null)
        {
            if (options == null)
            {
                options = new XmlOptions();
            }

            XElement retVal = new XElement("Node",
                new XAttribute("IsToken", false),
                new XAttribute("IsTrivia", true),
                new XAttribute("Kind", token.GetKind()),
                new XAttribute("IsMissing", token.IsMissing));

            if (options.Spans)
            {
                retVal.Add(@"<Span Start=<%= token.SpanStart %> End=<%= token.Span.End %> Length=<%= token.Span.Length %>/>");
                retVal.Add(@"<FullSpan Start=<%= token.FullSpan.Start %> End=<%= token.FullSpan.End %> Length=<%= token.FullSpan.Length %>/>");
            }

            if (options.Text)
            {
                retVal.Add(@"<Text><%= New XCData(token.GetText()) %></Text>");
            }

            if (options.ReflectionInfo)
            {
                AddInfo(token, retVal, options);
            }

            if (options.Errors)
            {
                if (syntaxTree.GetDiagnostics(token).Any())
                {
                    AddErrors(retVal, syntaxTree.GetDiagnostics(token), options);
                }
            }

            if (options.Trivia)
            {
                if (token.LeadingTrivia.Any())
                {
                    retVal.Add(@"<LeadingTrivia><%= From t In token.LeadingTrivia Select t.ToXml(syntaxTree, options) %></LeadingTrivia>");
                }

                if (token.TrailingTrivia.Any())
                {
                    retVal.Add(@"<TrailingTrivia><%= From t In token.TrailingTrivia Select t.ToXml(syntaxTree, options) %></TrailingTrivia>");
                }
            }

            return retVal;
        }

        public static XElement ToXml(this SyntaxTrivia trivia, SyntaxTree syntaxTree, XmlOptions options = null)
        {
            if (options == null)
            {
                options = new XmlOptions();
            }

            XElement retVal = new XElement("Node",
                new XAttribute("IsToken", false),
                new XAttribute("IsTrivia", true),
                new XAttribute("Kind", trivia.GetKind()),
                new XAttribute("IsMissing", false));
            if (options.Spans)
            {
                retVal.Add(@"<Span Start=<%= trivia.SpanStart %> End=<%= trivia.Span.End %> Length=<%= trivia.Span.Length %>/>");
                retVal.Add(@"<FullSpan Start=<%= trivia.FullSpan.Start %> End=<%= trivia.FullSpan.End %> Length=<%= trivia.FullSpan.Length %>/>");
            }

            if (options.Text)
            {
                retVal.Add(@"<Text><%= New XCData(trivia.GetText()) %></Text>");
            }

            if (options.ReflectionInfo)
            {
                AddInfo(trivia, retVal, options);
            }

            if (options.Errors)
            {
                if (syntaxTree.GetDiagnostics(trivia).Any())
                {
                    AddErrors(retVal, syntaxTree.GetDiagnostics(trivia), options);
                }
            }

            if (trivia.HasStructure)
            {
                retVal.Add(trivia.GetStructure().ToXml(syntaxTree, options));
            }

            return retVal;
        }
    }
}
