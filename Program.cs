using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace XamlSort
{
    /// <summary>
    ///  Cleans up XAML Rules
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Cleans up XAML Rules
        /// </summary>
        public static void Main(string directory = null)
        {
            directory ??= Environment.CurrentDirectory;
            foreach (string file in Directory.EnumerateFiles(directory, "*.xaml"))
            {
                SortFile(file);
            }
        }

        private static void SortFile(string file)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText(file));
            doc.PreserveWhitespace = true;

            // We only do Rules
            if (!doc.DocumentElement.Name.Equals("Rule", StringComparison.Ordinal)) return;

            string tempFile = Path.GetTempFileName();
            try
            {
                using (XmlWriter writer = XmlWriter.Create(tempFile, new XmlWriterSettings
                {
                    // We'll do the whitespace
                    Indent = false,
                    WriteEndDocumentOnClose = true
                }))
                {
                    foreach (XmlNode comment in doc.ChildNodes)
                    {
                        if (comment is XmlDeclaration decl)
                        {
                            writer.WriteStartDocument();
                            writer.WriteWhitespace(Environment.NewLine);
                        }
                        else if (comment is XmlComment comm)
                        {
                            writer.WriteComment(comm.Value);
                            writer.WriteWhitespace(Environment.NewLine);
                        }
                        else
                        {
                            break;
                        }
                    }
                    WriteNode(writer, doc.DocumentElement, 1);
                }
                File.Delete(file);
                File.Move(tempFile, file);
                Console.WriteLine("Finished " + Path.GetFileName(file));
            }
            catch (Exception ex)
            {
                File.Delete(tempFile);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error cleaning " + Path.GetFileName(file) + ": " + ex.Message);
                Console.ResetColor();
            }
        }

        private static void WriteNode(XmlWriter writer, XmlElement node, int level)
        {
            var baseIndent = (level - 1) * 2;
            writer.WriteWhitespace(new string(' ', baseIndent));
            writer.WriteStartElement(node.Name, node.NamespaceURI);
            foreach (var att in Sort(node.Attributes))
            {
                writer.WriteAttributeString(att.Name, att.Value);
            }
            if (node.ChildNodes.Count > 0)
            {
                writer.WriteWhitespace(Environment.NewLine);
                foreach (var child in Sort(node.Name, node.ChildNodes))
                {
                    WriteNode(writer, child, level + 1);
                    if (level == 1)
                    {
                        writer.WriteWhitespace(Environment.NewLine);
                    }
                }
                writer.WriteWhitespace(new string(' ', (level - 1) * 2));
            }
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        private static IEnumerable<XmlElement> Sort(string name, XmlNodeList childNodes)
        {
            // don't sort enum values
            if (name.Equals("EnumProperty"))
            {
                foreach (XmlElement element in childNodes)
                {
                    yield return element;
                }
                yield break;
            }
            foreach (XmlElement element in childNodes.OfType<XmlElement>().Where(x => x.Name.StartsWith(name + ".", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name))
            {
                yield return element;
            }
            foreach (XmlElement element in childNodes.OfType<XmlElement>().Where(x => !x.Name.StartsWith(name + ".", StringComparison.OrdinalIgnoreCase)).Where(x => x.HasAttribute("Name")).OrderBy(x => x.Attributes["Name"].Value))
            {
                yield return element;
            }
            foreach (XmlElement element in childNodes.OfType<XmlElement>().Where(x => !x.Name.StartsWith(name + ".", StringComparison.OrdinalIgnoreCase)).Where(x => !x.HasAttribute("Name")).OrderBy(x => x.Name))
            {
                yield return element;
            }
        }

        private static IEnumerable<XmlAttribute> Sort(XmlAttributeCollection attributes)
        {
            foreach (XmlAttribute att in attributes)
            {
                if (att.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    yield return att;
                    break;
                }
            }

            foreach (XmlAttribute att in attributes.Cast<XmlAttribute>().Where(x => !x.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name))
            {
                yield return att;
            }
        }
    }
}
