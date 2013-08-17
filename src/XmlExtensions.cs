using System;
using System.Xml;

namespace CounterCreator
{
    public static class XmlExtensions
    {
        public static string GetAttribute(this XmlNode node, string attributeName)
        {
            if (node == null || node.Attributes[attributeName] == null)
            {
                return null;
            }

            return node.Attributes[attributeName].Value;
        }

        public static T ParseToEnum<T>(this string enumLabel)
        {
            return (T)Enum.Parse(typeof(T), enumLabel);
        }

        public static XmlAttribute CreateAttribute(this XmlDocument doc, string attributeName, string localName, string namespaceUri, string value)
        {
            XmlAttribute attribute;

            if (localName == null && namespaceUri == null)
            {
                attribute = doc.CreateAttribute(attributeName);
            }
            else if (localName == null)
            {
                attribute = doc.CreateAttribute(attributeName, namespaceUri);
            }
            else
            {
                attribute = doc.CreateAttribute(attributeName, localName, namespaceUri);
            }

            attribute.Value = value;
            return attribute;
        }
    }
}