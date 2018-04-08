using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NSDMasterInventorySF.io
{
	public class ConfigGenerator
	{
		public static void WriteExternalAppConfig(string configFilePath, IDictionary<string, string> userConfiguration)
		{
			using (var xw = new XmlTextWriter(configFilePath, Encoding.UTF8))
			{
				xw.Formatting = Formatting.Indented;
				xw.Indentation = 4;
				xw.WriteStartDocument();
				xw.WriteStartElement("appSettings");

				foreach (KeyValuePair<string, string> pair in userConfiguration)
				{
					xw.WriteStartElement("add");
					xw.WriteAttributeString("key", pair.Key);
					xw.WriteAttributeString("value", pair.Value);
					xw.WriteEndElement();
				}

				xw.WriteEndElement();
				xw.WriteEndDocument();
			}
		}
	}
}