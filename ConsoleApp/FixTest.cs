using System;
using System.IO;
using System.Text;
using System.Xml;

namespace ConsoleApp
{
    public class FixTest
    {
        public static void Run()
        {
            string file = "TT-FIX42.xml";
            string className = file.Replace("-",string.Empty).Replace("\\",string.Empty).Replace("/", string.Empty).Replace(":", string.Empty);
            StringBuilder generation = new StringBuilder();
            using(var fs=new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using(XmlReader xr = XmlReader.Create(fs))
            {
                while (xr.Read())
                {
                    if(xr.NodeType == XmlNodeType.Element && xr.LocalName == "fields")
                    {
                        StringBuilder fieldEnumBuilder = new StringBuilder();
                        //create tags class
                        generation
                            .AppendLine("namespace temp {")
                            .Append("   public enum ").Append(className).Append("Tags").AppendLine("{")
                            ;
                        ReadFields(xr, generation, fieldEnumBuilder);

                        //close tags class
                        generation
                            .AppendLine("   }")
                            .AppendLine(fieldEnumBuilder.ToString())
                            .AppendLine("}");
                    }
                }
            }
            Console.WriteLine(generation.ToString());
        }

        private static void ReadFields(XmlReader xr, StringBuilder generation, StringBuilder fieldEnumBuilder)
        {
            bool isEnumStart = true;
            string name = string.Empty;
            while(xr.Read() && (xr.NodeType != XmlNodeType.EndElement || xr.Depth > 1))
            {
                if(xr.NodeType== XmlNodeType.Element && xr.LocalName == "field")
                {
                    if (!isEnumStart)
                    {
                        fieldEnumBuilder
                            .AppendLine("   }");
                    }
                    isEnumStart = true;


                    (string name2, string val) = ReadAttributes(xr, "name", "number");
                    name = name2;
                    generation.Append("     ").Append(name).Append(" = ").Append(val).AppendLine(",");

                }
                if (xr.NodeType == XmlNodeType.Element && xr.LocalName == "value")
                {
                    if (isEnumStart)
                    {
                        isEnumStart = false;
                        fieldEnumBuilder
                            .Append("   public enum ").Append(name).AppendLine("Enum {");
                    }

                    (string eName, string eValue) = ReadAttributes(xr, "description", "enum");
                    //here are char values, so we assign them as int
                    fieldEnumBuilder.Append("       ").Append(eName).Append(" = ").Append((int)eValue[0]).AppendLine(",");
                }

            }
        }

        private static (string eName, string eValue) ReadAttributes(XmlReader xr, string nameField, string valueField)
        {
            string name = string.Empty;
            string val = string.Empty;
            int cnt = xr.AttributeCount;
            for (int i = 0; i < cnt; i++)
            {
                xr.MoveToAttribute(i);
                if (xr.LocalName == nameField)
                    name = xr.Value;
                if (xr.LocalName == valueField)
                    val = xr.Value;
            }
            return (name, val);
        }
    }

}
