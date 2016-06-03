// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Roslyn.VisualStudio.Test.Utilities.Common
{
    [Serializable]
    public class Signature : IEquatable<Signature>
    {
        public string Content { get; set; }
        public Parameter CurrentParameter { get; set; }
        public string Documentation { get; set; }
        public Parameter[] Parameters { get; set; }
        public string PrettyPrintedContent { get; set; }

        public Signature()
        {
        }

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

        private bool ParametersEqual(Parameter[] otherParameters)
        {
            if (this.Parameters.Length != otherParameters.Length)
            {
                return false;
            }

            for (int i = 0; i < this.Parameters.Length; i++)
            {
                if (!this.Parameters[i].Equals(otherParameters[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Equals(Signature other)
        {
            return other != null
                && this.Content == other.Content
                && Equals(CurrentParameter, other.CurrentParameter)
                && this.PrettyPrintedContent == other.PrettyPrintedContent
                && this.Documentation == other.Documentation
                && ParametersEqual(other.Parameters);
        }

        public override bool Equals(object obj)
        {
            Signature other = obj as Signature;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return (Content ?? string.Empty).GetHashCode()
                 ^ (Documentation ?? string.Empty).GetHashCode()
                 ^ (PrettyPrintedContent ?? string.Empty).GetHashCode()
                 ^ (CurrentParameter ?? new Parameter()).GetHashCode();
        }

        public override string ToString()
        {
            return string.Join(
                Environment.NewLine,
                Content,
                Documentation,
                PrettyPrintedContent,
                (CurrentParameter ?? new Parameter()).ToString(),
                Parameters != null ? string.Join(",", Parameters.Select(p => p.ToString())) : "No parameters");
        }
    }
}
