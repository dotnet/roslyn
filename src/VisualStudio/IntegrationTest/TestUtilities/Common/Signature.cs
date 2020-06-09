// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    [Serializable]
    public class Signature : IEquatable<Signature>
    {
        public string Content { get; set; }

        public Parameter CurrentParameter { get; set; }

        public string Documentation { get; set; }

        public Parameter[] Parameters { get; set; }

        public string PrettyPrintedContent { get; set; }

        public Signature() { }

        public Signature(ISignature actual)
        {
            Content = actual.Content;
            Documentation = actual.Documentation;
            Parameters = actual.Parameters.Select(p => new Parameter(p)).ToArray();

            if (actual.CurrentParameter != null)
            {
                CurrentParameter = new Parameter(actual.CurrentParameter);
            }

            PrettyPrintedContent = actual.PrettyPrintedContent;
        }

        public bool Equals(Signature other)
            => other != null
            && Comparison.AreStringValuesEqual(Content, other.Content)
            && Equals(CurrentParameter, other.CurrentParameter)
            && Comparison.AreStringValuesEqual(PrettyPrintedContent, other.PrettyPrintedContent)
            && Comparison.AreStringValuesEqual(Documentation, other.Documentation)
            && Comparison.AreArraysEqual(Parameters, other.Parameters);

        public override bool Equals(object obj)
            => Equals(obj as Signature);

        public override int GetHashCode()
            => Hash.Combine(Content, Hash.Combine(Documentation, Hash.Combine(PrettyPrintedContent, Hash.Combine(CurrentParameter, 0))));

        public override string ToString()
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(Content))
            {
                builder.AppendLine(Content);
            }

            if (!string.IsNullOrEmpty(Documentation))
            {
                builder.AppendLine(Documentation);
            }

            if (!string.IsNullOrEmpty(PrettyPrintedContent))
            {
                builder.AppendLine(PrettyPrintedContent);
            }

            if (CurrentParameter != null)
            {
                builder.AppendLine(CurrentParameter.ToString());
            }

            if (Parameters?.Length > 0)
            {
                builder.Append(string.Join(",", Parameters.Select(p => p.ToString())));
            }
            else
            {
                builder.Append("No parameters");
            }

            return builder.ToString();
        }
    }
}
