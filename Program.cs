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
        private static bool _autoIndent;

        /// <summary>
        /// Cleans up XAML Rules
        /// </summary>
        public static void Main(string directory = null, bool autoIndent = false)
        {
            _autoIndent = autoIndent;

            directory ??= Environment.CurrentDirectory;
            foreach (string file in Directory.EnumerateFiles(directory, "*.xaml"))
            {
                SortFile(file);
            }
        }

        private static void SortFile(string file)
        {
            var doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText(file));
            doc.PreserveWhitespace = true;

            // We only do Rules
            if (!doc.DocumentElement.Name.Equals("Rule", StringComparison.Ordinal)) return;

            string tempFile = Path.GetTempFileName();
            try
            {
                using (var writer = XmlWriter.Create(tempFile, new XmlWriterSettings
                {
                    // We'll do the whitespace
                    Indent = _autoIndent,
                    NewLineOnAttributes = _autoIndent,
                    WriteEndDocumentOnClose = true
                }))
                {
                    foreach (XmlNode node in doc.ChildNodes)
                    {
                        if (node is XmlDeclaration declaration)
                        {
                            // annoyingly passing false to WriteStartDocument writes out 'standalone="no"' so we need to call a different overload
                            // to preserve the lack of a declaration
                            if (declaration.Standalone.Length == 0)
                            {
                                writer.WriteStartDocument();
                            }
                            else
                            {
                                writer.WriteStartDocument(declaration.Standalone.Equals("yes", StringComparison.OrdinalIgnoreCase));
                            }
                            WriteNewLine(writer);
                        }
                        else if (node is XmlComment comment)
                        {
                            writer.WriteComment(comment.Value);
                            WriteNewLine(writer);
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
            var indent = (level - 1) * 2;
            WriteIndent(writer, indent);
            writer.WriteStartElement(node.Name, node.NamespaceURI);
            foreach (var att in Sort(node.Attributes))
            {
                writer.WriteAttributeString(att.Name, att.Value);
            }
            if (node.ChildNodes.Count > 0)
            {
                WriteNewLine(writer);
                foreach (var child in Sort(node.Name, node.ChildNodes))
                {
                    WriteNode(writer, child, level + 1);
                    if (level == 1)
                    {
                        WriteNewLine(writer);
                    }
                }
                WriteIndent(writer, indent);
            }
            writer.WriteEndElement();
            WriteNewLine(writer);
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
            foreach (var element in childNodes.OfType<XmlElement>().Where(x => x.Name.StartsWith(name + ".", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name))
            {
                yield return element;
            }
            foreach (var element in childNodes.OfType<XmlElement>().Where(x => !x.Name.StartsWith(name + ".", StringComparison.OrdinalIgnoreCase)).Where(x => x.HasAttribute("Name")).OrderBy(x => x.Attributes["Name"].Value))
            {
                yield return element;
            }
            foreach (var element in childNodes.OfType<XmlElement>().Where(x => !x.Name.StartsWith(name + ".", StringComparison.OrdinalIgnoreCase)).Where(x => !x.HasAttribute("Name")).OrderBy(x => x.Name))
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

            foreach (var att in attributes.Cast<XmlAttribute>().Where(x => !x.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name))
            {
                yield return att;
            }
        }

        private static void WriteIndent(XmlWriter writer, int indent)
        {
            if (!_autoIndent)
            {
                writer.WriteWhitespace(new string(' ', indent));
            }
        }

        private static void WriteNewLine(XmlWriter writer)
        {
            if (!_autoIndent)
            {
                writer.WriteWhitespace(Environment.NewLine);
            }
        }
    }
}
