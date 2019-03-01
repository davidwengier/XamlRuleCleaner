using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace XamlRuleCleaner
{
    internal static class XmlWriterExtensions
    {
        internal static void SetState(this XmlWriter writer, XmlWriterState state)
        {
            var type = writer.GetType();
            var fieldInfo = type.GetField("currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo.SetValue(writer, (int)state, System.Reflection.BindingFlags.SetField, Type.DefaultBinder, null);
        }

        internal static void WriteIndent(this XmlWriter writer, int indent)
        {
            writer.WriteWhitespace(new string(' ', indent));
        }

        internal static void WriteNewLine(this XmlWriter writer)
        {
            writer.WriteWhitespace(Environment.NewLine);
        }
    }
}
