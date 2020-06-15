using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace AzureFunction
{
    public static class UpdateFeed
    {
        public static XNamespace AtomNs => XNamespace.Get("http://www.w3.org/2005/Atom");
        public static XNamespace GalleryNs => XNamespace.Get("http://schemas.microsoft.com/developer/vsx-syndication-schema/2010");
        public static XNamespace VsixNs => XNamespace.Get("http://schemas.microsoft.com/developer/vsx-schema/2011");

        [FunctionName("update")]
        public static void Run(
            [BlobTrigger("vsix/{feature}/{file}.vsix", Connection = "FeedStorageConnectionString")] Stream blob,
            string feature, string file,
            [Blob("vsix/{feature}/atom.xml", FileAccess.Read, Connection = "FeedStorageConnectionString")] Stream currentFeed,
            [Blob("vsix/{feature}/atom.xml", FileAccess.Write, Connection = "FeedStorageConnectionString")] Stream updatedFeed)
        {
            var feedTitle = $"Roslyn Feature: { CultureInfo.CurrentCulture.TextInfo.ToTitleCase(feature) }";
            var feedId = "roslyn.feature." + feature;

            var blobClient = new BlobServiceClient(Environment.GetEnvironmentVariable("FeedStorageConnectionString"));
            var baseUrl = blobClient.Uri.OriginalString;
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";

            baseUrl += "vsix/" + feature;

            XElement atom;
            if (currentFeed == null)
            {
                atom = new XElement(AtomNs + "feed",
                    new XElement(AtomNs + "title", new XAttribute("type", "text"), feedTitle),
                    new XElement(AtomNs + "id", feedId)
                );
            }
            else
            {
                try
                {
                    atom = XDocument.Load(currentFeed).Root;
                    atom.Element(AtomNs + "title")?.SetValue(feedTitle);
                    atom.Element(AtomNs + "id")?.SetValue(feedId);
                }
                catch (XmlException)
                {
                    atom = new XElement(AtomNs + "feed",
                        new XElement(AtomNs + "title", new XAttribute("type", "text"), feedTitle),
                        new XElement(AtomNs + "id", feedId)
                    );
                }
            }

            var updated = atom.Element(AtomNs + "updated");
            if (updated == null)
            {
                updated = new XElement(AtomNs + "updated");
                atom.Add(updated);
            }

            updated.Value = XmlConvert.ToString(DateTimeOffset.UtcNow);

            using var archive = new ZipArchive(blob, ZipArchiveMode.Read);
            var zipEntry = archive.GetEntry("extension.vsixmanifest");
            if (zipEntry != null)
            {
                using var stream = zipEntry.Open();
                var manifest = XDocument.Load(stream).Root;
                var metadata = manifest.Element(VsixNs + "Metadata");
                var identity = metadata.Element(VsixNs + "Identity");
                var id = identity.Attribute("Id").Value;
                var version = identity.Attribute("Version").Value;

                var entry = atom.Elements(AtomNs + "entry").FirstOrDefault(x => x.Element(AtomNs + "id")?.Value == id);
                if (entry != null)
                    entry.Remove();

                entry = new XElement(AtomNs + "entry",
                    new XElement(AtomNs + "id", id),
                    new XElement(AtomNs + "title", new XAttribute("type", "text"), metadata.Element(VsixNs + "DisplayName").Value),
                    new XElement(AtomNs + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("href", $"{baseUrl}/{file}.vsix")),
                    new XElement(AtomNs + "summary", new XAttribute("type", "text"), metadata.Element(VsixNs + "Description").Value),
                    new XElement(AtomNs + "published", XmlConvert.ToString(DateTimeOffset.UtcNow)),
                    new XElement(AtomNs + "updated", XmlConvert.ToString(DateTimeOffset.UtcNow)),
                    new XElement(AtomNs + "author",
                        new XElement(AtomNs + "name", identity.Attribute("Publisher").Value)),
                    new XElement(AtomNs + "content",
                        new XAttribute("type", "application/octet-stream"),
                        new XAttribute("src", $"{baseUrl}/{file}.vsix"))
                );

                var vsix = new XElement(GalleryNs + "Vsix",
                    new XElement(GalleryNs + "Id", id),
                    new XElement(GalleryNs + "Version", version),
                    new XElement(GalleryNs + "References")
                );

                entry.Add(vsix);
                atom.AddFirst(entry);

                using var writer = XmlWriter.Create(updatedFeed, new XmlWriterSettings { Indent = true });
                atom.WriteTo(writer);
                writer.Flush();
            }
        }
    }
}
