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
    class CodeDomEntity
    {
        CodeCompileUnit targetUnit;
        CodeTypeDeclaration targetClass;
        private string outputFileName = string.Empty;
        private JToken structualType = null;
        private string targetNamespace = string.Empty;

        public CodeDomEntity(JToken structualType, string targetNamespace)
        {
            this.targetNamespace = targetNamespace;
            outputFileName = string.Format("{0}.cs", structualType["shortName"].Value<string>());
            this.structualType = structualType;
            targetUnit = new CodeCompileUnit();
            CodeNamespace breezeEntities = new CodeNamespace(targetNamespace);
            breezeEntities.Imports.Add(new CodeNamespaceImport("System"));
            breezeEntities.Imports.Add(new CodeNamespaceImport("Breeze.Sharp"));
            breezeEntities.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            targetClass = new CodeTypeDeclaration(structualType["shortName"].Value<string>());
            if (structualType["baseTypeName"] != null)
            {
                //use base class from the base type (it will still be BaseEntity)
                string baseType = structualType["baseTypeName"].Value<string>();
                targetClass.BaseTypes.Add(new CodeTypeReference(baseType.Substring(0, baseType.IndexOf(':'))));
            }
            else
            {
                //this entity type has no base class so use BaseEntity
                targetClass.BaseTypes.Add(new CodeTypeReference("BaseEntity"));
            }

            targetClass.IsClass = true;
            targetClass.TypeAttributes = TypeAttributes.Public;
            breezeEntities.Types.Add(targetClass);
            targetUnit.Namespaces.Add(breezeEntities);
        }

        public void AddDataProperties()
        {
            foreach (var property in structualType["dataProperties"])
            {
                CodeMemberProperty entityProperty = new CodeMemberProperty();
                entityProperty.Attributes = MemberAttributes.Public;
                entityProperty.Name = property["nameOnServer"].Value<string>();
                entityProperty.HasGet = true;
                entityProperty.HasSet = true;
                entityProperty.GetStatements.Add(new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(
                    new CodeThisReferenceExpression(), entityProperty.Name)));
                entityProperty.SetStatements.Add(new CodeAssignStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), entityProperty.Name),
                    new CodePropertySetValueReferenceExpression()));
                var dataType = property["dataType"].Value<string>();
                try
                {
                    if (property["isNullable"].Value<bool>() && dataType != "String")
                    {
                        //this is a nullable type so create it as nullable
                        dataType = string.Format("Nullable<{0}>", dataType);
                    }
                }
                catch (Exception)
                {
                    
                }
                if (dataType.Contains("NHibernate.Type.EnumType")) //NHibernate specific code until a new version of breeze metadata would be released
                {
                    HandleNHEnum(entityProperty, dataType);
                }
                else
                {
                    entityProperty.Type = new CodeTypeReference(dataType);
                }
                targetClass.Members.Add(entityProperty);
            }
        }

        private void HandleNHEnum(CodeMemberProperty entityProperty, string dataType)
        {
            //extract nhibernate enum type name
            try
            {
                if (dataType.Contains('+')) //this means it's not under .net framework assembly??? 
                {
                    var parsedDataType = dataType.Substring(dataType.LastIndexOf('+') + 1, dataType.IndexOf(',', dataType.LastIndexOf('+')) - dataType.LastIndexOf('+') - 1);
                    entityProperty.Type = new CodeTypeReference(string.Format("{0}.{1}", targetClass, parsedDataType));
                }
                else
                {
                    //this means it must be a system assembly enum
                    string parsedDataType = dataType.Substring(dataType.LastIndexOf('[') + 1, dataType.IndexOf(',', dataType.LastIndexOf('[')) - dataType.LastIndexOf('[') - 1);
                    string enumNamespace = parsedDataType.Substring(0, parsedDataType.LastIndexOf('.'));
                    entityProperty.Type = new CodeTypeReference(parsedDataType);
                    //add the assembly to this class
                    targetUnit.Namespaces.Add(new CodeNamespace(enumNamespace));

                }
            }
            catch (Exception)
            {
            }
        }

        public void AddNavigationProperties()
        {
            foreach (var property in structualType["navigationProperties"])
            {
                CodeMemberProperty entityProperty = new CodeMemberProperty();
                entityProperty.Attributes = MemberAttributes.Public;
                entityProperty.Name = property["nameOnServer"].Value<string>();
                entityProperty.HasGet = true;
                entityProperty.HasSet = true;
                entityProperty.GetStatements.Add(new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(
                    new CodeThisReferenceExpression(), entityProperty.Name)));
                entityProperty.SetStatements.Add(new CodeAssignStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), entityProperty.Name),
                    new CodePropertySetValueReferenceExpression()));

                var entityTypeName = property["entityTypeName"].Value<string>().Split(':')[0];
                if (property["isScalar"].Value<bool>())
                {
                    entityProperty.Type = new CodeTypeReference(entityTypeName);
                }
                else
                {
                    entityProperty.Type = new CodeTypeReference(string.Format("IEnumerable<{0}>", entityTypeName));
                }
                targetClass.Members.Add(entityProperty);
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
