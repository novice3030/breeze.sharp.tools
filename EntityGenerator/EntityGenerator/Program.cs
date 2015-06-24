using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace breeze.sharp.tools.EntityGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var excludedEntities = ConfigurationManager.AppSettings["ExcludedEntities"].Split(',').ToList();
            var excludedNameSpaces = ConfigurationManager.AppSettings["ExcludedNamespaces"].Split(',').ToList();
            DisplayGreetingsAndNotes();
            Console.WriteLine("Please enter the output directory all the files will be generated to");
            var outputDirectoryPath = Console.ReadLine();
            var outputDirectory = new DirectoryInfo(outputDirectoryPath);
            Console.WriteLine("Please enter the target namespace");
            var targetNamespace = Console.ReadLine();
            Console.WriteLine("Please enter the path for metadata file generated from breeze server");
            var metadataPath = Console.ReadLine();

            var metadataFile = new FileInfo(metadataPath);
            if (!metadataFile.Exists)
            {
                Console.WriteLine("Metadata file was not found! please make sure the path is correct and that files exists");
                Console.ReadKey();
                return;
            }
            else
            {
                var metadata = string.Empty;
                using (StreamReader reader = File.OpenText(metadataFile.FullName))
                {
                    metadata = reader.ReadToEnd();
                }
                try
                {
                    if (!outputDirectory.Exists)
                    {
                        outputDirectory.Create();
                    }

                    var jsonMetadata = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(metadata);
                    var structualTypes = jsonMetadata["structuralTypes"];

                    foreach (var structualType in structualTypes)
                    {
                        var entityType = structualType["shortName"].Value<string>();
                        var sourceNamespace = structualType["namespace"].Value<string>();
                        if (!excludedEntities.Contains(entityType) && !excludedNameSpaces.Contains(sourceNamespace))
                        {
                            Console.WriteLine(string.Format("generating {0}.{1}", entityType, sourceNamespace));
                            try
                            {
                                CodeDomEntity entity = new CodeDomEntity(structualType, targetNamespace);
                                entity.AddDataProperties();
                                entity.AddNavigationProperties();
                                entity.GenerateCSharpCode(string.Format("{0}{1}.cs", outputDirectory.FullName, entityType));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("error generating {0}.{1} {2}", entityType, sourceNamespace, ex.Message);
                                Console.WriteLine("would you like to continue? (y/n)");
                                var keyPressed = Console.ReadKey();
                                if (keyPressed.Key == ConsoleKey.Y)
                                {
                                    continue;
                                }
                                Console.ReadKey();
                                return;
                            }
                        }
                    }

                    Console.WriteLine("generating enums.cs");
                    CodeDomEnum codeDomEnum = new CodeDomEnum(jsonMetadata["enumTypes"], targetNamespace);
                    codeDomEnum.AddEnums();
                    codeDomEnum.GenerateCSharpCode(string.Format("{0}Enums.cs", outputDirectory.FullName));

                    Console.WriteLine("All done!");
                    Console.ReadKey();

                }
                catch (Exception ex)
                {
                    Console.WriteLine("unable to convert metadata to json!: " + ex.Message);
                    Console.ReadKey();
                    return;
                }
            }
        }

        private static void DisplayGreetingsAndNotes()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("This tool is used to generate C# classes from Breeze server metadata.");
            Console.WriteLine("It is currently in early beta stages and has been tested only with NHibernate");
            Console.WriteLine("EF support has not been tested yet!");

        }
    }
}
