// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// REPL session buffer: input, output, or prompt.
    /// </summary>
    [DataContract]
    internal struct BufferBlock
    {
        [DataMember(Name = "kind")]
        internal readonly ReplSpanKind Kind;

        [DataMember(Name = "content")]
        internal readonly string Content;

        internal BufferBlock(ReplSpanKind kind, string content)
        {
            Kind = kind;
            Content = content;
        }

        internal static string Serialize(BufferBlock[] blocks)
        {
            var serializer = new DataContractJsonSerializer(typeof(BufferBlock[]));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, blocks);
                return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            }
        }

        internal static BufferBlock[] Deserialize(string str)
        {
            var serializer = new DataContractJsonSerializer(typeof(BufferBlock[]));
            var bytes = Encoding.UTF8.GetBytes(str);
            using (var stream = new MemoryStream(bytes))
            {
                var obj = serializer.ReadObject(stream);
                return (BufferBlock[])obj;
            }
        }
    }
}
