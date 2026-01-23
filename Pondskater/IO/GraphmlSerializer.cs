using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Pondskater.IO
{
    internal static class GraphmlSerializer
    {
        internal static string ToXml(Graphml graph)
        {
            var serializer = new XmlSerializer(typeof(Graphml));
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = false,
                Indent = false
            };

            using var ms = new MemoryStream();
            using (var writer = XmlWriter.Create(ms, settings))
            {
                serializer.Serialize(writer, graph);
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
