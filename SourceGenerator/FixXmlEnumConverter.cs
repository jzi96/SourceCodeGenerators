using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Text;
namespace janzi.Projects.SourceCodeGenerators
{
    [Generator]
    public class FixXmlEnumConverter : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        // Adds a class to the `CSV` namespace for each `csv` file passed in. The class has a static property
        // named `All` that returns the list of strongly typed objects generated on demand at first access.
        // There is the slight chance of a race condition in a multi-thread program, but the result is relatively benign
        // , loading the collection multiple times instead of once. Measures could be taken to avoid that.
        public static string GenerateClassFile(string namespaceName, string className, string csvText, bool cacheObjects)
        {
            //System.Diagnostics.Debugger.Launch();
            StringBuilder generation = new StringBuilder();
            using (var fs = new FileStream(csvText, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (XmlReader xr = XmlReader.Create(fs))
            {
                while (xr.Read())
                {
                    if (xr.NodeType == XmlNodeType.Element && xr.LocalName == "fields")
                    {
                        StringBuilder fieldEnumBuilder = new StringBuilder();
                        //create tags class
                        generation
                            .Append("namespace ").Append(namespaceName).AppendLine(" {")
                            .AppendLine("    [System.CodeDom.Compiler.GeneratedCode(\"janzi.Projects.SourceCodeGenerators\", \"1.0.0.0\")]")
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
            return generation.ToString();
        }
        private static void ReadFields(XmlReader xr, StringBuilder generation, StringBuilder fieldEnumBuilder)
        {
            bool isEnumStart = true;
            string name = string.Empty;
            while (xr.Read() && (xr.NodeType != XmlNodeType.EndElement || xr.Depth > 1))
            {
                if (xr.NodeType == XmlNodeType.Element && xr.LocalName == "field")
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
                            .AppendLine("       [System.CodeDom.Compiler.GeneratedCode(\"janzi.Projects.SourceCodeGenerators\", \"1.0.0.0\")]")
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

        static string StringToValidPropertyName(string s)
        {
            s = s.Trim();
            s = char.IsLetter(s[0]) ? char.ToUpper(s[0]) + s.Substring(1) : s;
            s = char.IsDigit(s.Trim()[0]) ? "_" + s : s;
            s = new string(s.Select(ch => char.IsDigit(ch) || char.IsLetter(ch) ? ch : '_').ToArray());
            return s;
        }

        static IEnumerable<(string, string)> SourceFilesFromAdditionalFile(string namespaceName, bool cacheObjects, AdditionalText file)
        {
            string className = Path.GetFileNameWithoutExtension(file.Path).Replace("-", string.Empty).Replace("\\", string.Empty).Replace("/", string.Empty).Replace(":", string.Empty);
            string csvText = file.Path;
            return new (string, string)[] { (className, GenerateClassFile(namespaceName, className, csvText,  cacheObjects)) };
        }

        static IEnumerable<(string, string)> SourceFilesFromAdditionalFiles(IEnumerable<(string namespaceName, bool cacheObjects, AdditionalText file)> pathsData)
            => pathsData.SelectMany(d => SourceFilesFromAdditionalFile(d.namespaceName, d.cacheObjects, d.file));

        static IEnumerable<(string namespaceName, bool cacheObjects, AdditionalText file)> GetLoadOptions(GeneratorExecutionContext context)
        {
            string namespaceName = context.Compilation.Assembly.NamespaceNames.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
            namespaceName = namespaceName ?? "Fix";


            foreach (AdditionalText file in context.AdditionalFiles)
            {
                if (Path.GetExtension(file.Path).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    // are there any options for it?
                    context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.additionalfiles.CacheObjects", out string? cacheObjectsString);
                    bool.TryParse(cacheObjectsString, out bool cacheObjects);

                    yield return (namespaceName, cacheObjects, file);
                }
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            IEnumerable<(string namespaceName, bool cacheObjects, AdditionalText file)> options = GetLoadOptions(context);
            IEnumerable<(string, string)> nameCodeSequence = SourceFilesFromAdditionalFiles(options);
            foreach ((string name, string code) in nameCodeSequence)
                context.AddSource($"FIX_{name}", SourceText.From(code, Encoding.UTF8));
        }
    }
}
