using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace janzi.Projects.SourceCodeGenerators
{
    internal static class Helper
    {
        private static readonly string[] IgnoreNamespaces = new[] { "XamlGeneratedNamespace" };
        public static string GetNamespace(GeneratorExecutionContext context, string fallbackNamespace)
        {
            string ns = context.Compilation.Assembly.NamespaceNames.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c) && !IgnoreNamespaces.Any(t=>t == c));
            ns = ns ?? fallbackNamespace;
            return ns;
        }

    }
}
