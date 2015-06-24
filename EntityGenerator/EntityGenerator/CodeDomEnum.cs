using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace breeze.sharp.tools.EntityGenerator
{
    class CodeDomEnum
    {
        private JToken enums = null;
        CodeCompileUnit targetUnit;
        CodeTypeDeclaration targetClass;

        public CodeDomEnum(JToken enums, string targetNamespace)
        {
            this.enums = enums;
            targetUnit = new CodeCompileUnit();
            CodeNamespace breezeEnums = new CodeNamespace(targetNamespace);
            breezeEnums.Imports.Add(new CodeNamespaceImport("System"));
            breezeEnums.Imports.Add(new CodeNamespaceImport("Breeze.Sharp"));
            targetClass = new CodeTypeDeclaration("Enums");

            targetClass.IsClass = true;
            targetClass.TypeAttributes = TypeAttributes.Public;
            breezeEnums.Types.Add(targetClass);
            targetUnit.Namespaces.Add(breezeEnums);
        }

        public void AddEnums()
        {
            foreach (var breezeEnum in enums)
            {
                string shortName = breezeEnum["shortName"].Value<string>();

                CodeTypeDeclaration type = new CodeTypeDeclaration(shortName);
                type.IsEnum = true;
                type.TypeAttributes = TypeAttributes.Public;

                foreach (var value in breezeEnum["values"].Values<string>())
                {
                    CodeMemberField f = new CodeMemberField(shortName, value);
                    type.Members.Add(f);
                }

                targetClass.Members.Add(type);
            }
        }

        public void GenerateCSharpCode(string fileName)
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";
            using (StreamWriter sourceWriter = new StreamWriter(fileName))
            {
                provider.GenerateCodeFromCompileUnit(
                    targetUnit, sourceWriter, options);
            }
        }
    }
}
