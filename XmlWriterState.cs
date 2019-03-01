using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XamlRuleCleaner
{
    // from http://index/?query=XmlWellFormedWriter&rightProject=System.Xml&file=System%5CXml%5CCore%5CXmlWellFormedWriter.cs&line=95
    internal enum XmlWriterState
    {
        Element = 3,
        Content = 4,
    }
}
