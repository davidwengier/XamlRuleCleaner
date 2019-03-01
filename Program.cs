using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace XamlRuleCleaner
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
                    Indent = false,
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
                            writer.WriteNewLine();
                        }
                        else if (node is XmlComment comment)
                        {
                            writer.WriteComment(comment.Value);
                            writer.WriteNewLine();
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
            writer.WriteIndent(indent);
            writer.WriteStartElement(node.Name, node.NamespaceURI);
            bool indentAttribute = false;
            int attributes = 0;
            foreach (var att in Sort(node.Attributes))
            {
                attributes++;
                if (indentAttribute)
                {
                    writer.WriteIndent(indent + 1 + node.Name.Length + 1);
                }
                // Set state to element to allow writing attributes (writing whitespace sets it to Content)
                writer.SetState(XmlWriterState.Element);
                writer.WriteAttributeString(att.Name, att.Value);
                // Set state back to content to allow writing whitespace without closing the element
                writer.SetState(XmlWriterState.Content);
                if (node.Attributes.Count > 1 && attributes != node.Attributes.Count)
                {
                    writer.WriteNewLine();
                    indentAttribute = true;
                }
            }
            // Set state to element to allow writing child nodes(writing whitespace sets it to Content)
            writer.SetState(XmlWriterState.Element);
            if (node.ChildNodes.Count > 0)
            {
                writer.WriteNewLine();
                foreach (var child in Sort(node.Name, node.ChildNodes))
                {
                    WriteNode(writer, child, level + 1);
                    if (level == 1)
                    {
                        writer.WriteNewLine();
                    }
                }
                writer.WriteIndent(indent);
            }
            writer.WriteEndElement();
            writer.WriteNewLine();
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
    }
}
