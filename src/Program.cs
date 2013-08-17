using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace CounterCreator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("The usage was incorrect.");
                return;
            }

            string filename = args[1];
            string method = args[0];

            try
            {
                var document = new XmlDocument();

                if (method.Equals("create", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!File.Exists(filename))
                    {
                        Console.Error.WriteLine("The file does not exist.");
                        return;
                    }

                    document.Load(filename);

                    foreach (XmlNode category in document.SelectNodes("/PerformanceCounters/Category"))
                    {
                        ProcessCategory(category);
                    }
                }
                else
                {
                    document.CreateXmlDeclaration("1.0", "utf-8", null);
                    var root = document.CreateElement("PerformanceCounters");
                    document.AppendChild(root);

                    foreach (var category in PerformanceCounterCategory.GetCategories())
                    {
                        root.AppendChild(ReadCategory(document, category));
                    }

                    document.Save(filename);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        private static XmlNode ReadCategory(XmlDocument document, PerformanceCounterCategory category)
        {
            Console.WriteLine("Reading category {0}", category.CategoryName);
            var categoryElement = document.CreateElement("Category");

            categoryElement.Attributes.Append(document.CreateAttribute("CategoryName", null, null, category.CategoryName));
            categoryElement.Attributes.Append(document.CreateAttribute("CategoryHelp", null, null, category.CategoryHelp));
            categoryElement.Attributes.Append(document.CreateAttribute("CategoryType", null, null, Enum.GetName(typeof(PerformanceCounterCategoryType), category.CategoryType)));

            PerformanceCounter[] counters;
            try
            {
                if (category.CategoryType == PerformanceCounterCategoryType.MultiInstance && category.GetInstanceNames().FirstOrDefault() != null)
                {
                    counters = category.GetCounters(category.GetInstanceNames().First());
                }
                else
                {
                    counters = category.GetCounters();
                }
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Could not read counters for category {0}.", category.CategoryName);
                throw;
            }

            if (counters.Length > 0)
            {
                var countersElement = document.CreateElement("Counters");
                categoryElement.AppendChild(countersElement);

                foreach (var counter in counters)
                {
                    countersElement.AppendChild(ReadCounter(document, counter));
                }
            }

            return categoryElement;
        }

        private static XmlNode ReadCounter(XmlDocument document, PerformanceCounter counter)
        {
            Console.WriteLine("    Reading counter {0}.", counter.CounterName);
            try
            {
                var counterElement = document.CreateElement("add");

                counterElement.Attributes.Append(document.CreateAttribute("CounterName", null, null, counter.CounterName));
                counterElement.Attributes.Append(document.CreateAttribute("CounterHelp", null, null, counter.CounterHelp));
                counterElement.Attributes.Append(document.CreateAttribute("CounterType", null, null, Enum.GetName(typeof(PerformanceCounterType), counter.CounterType)));

                return counterElement;
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Could not read counter {0} in category {1}.", counter.CounterName, counter.CategoryName);
                throw;
            }
        }

        private static void ProcessCategory(XmlNode categoryNode)
        {
            var collection = new CounterCreationDataCollection();
            foreach (XmlNode counter in categoryNode.SelectNodes("Counters/add"))
            {
                ProcessCounter(collection, counter);
            }

            try
            {
                if (PerformanceCounterCategory.Exists(categoryNode.GetAttribute("CategoryName")))
                {
                    PerformanceCounterCategory.Delete(categoryNode.GetAttribute("CategoryName"));
                    Console.WriteLine("Deleted existing category {0}", categoryNode.GetAttribute("CategoryName"));
                }

                if (collection.Count > 0)
                {
                    PerformanceCounterCategory.Create(
                        categoryNode.GetAttribute("CategoryName"),
                        categoryNode.GetAttribute("CategoryHelp"),
                        categoryNode.GetAttribute("CategoryType").ParseToEnum<PerformanceCounterCategoryType>(),
                        collection);
                    Console.WriteLine("Created category {0}", categoryNode.GetAttribute("CategoryName"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not create counter category {0}", categoryNode.GetAttribute("CategoryName"));
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
            }
        }

        private static void ProcessCounter(CounterCreationDataCollection collection, XmlNode counterNode)
        {
            try
            {
                CounterCreationData counter = new CounterCreationData
                    {
                        CounterName = counterNode.GetAttribute("CounterName"),
                        CounterHelp = counterNode.GetAttribute("CounterHelp"),
                        CounterType = counterNode.GetAttribute("CounterType").ParseToEnum<PerformanceCounterType>()
                    };
                collection.Add(counter);

                Console.WriteLine("Added counter {0}", counterNode.GetAttribute("CounterName"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not create counter {0}", counterNode.GetAttribute("CounterName"));
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
            }
        }
    }
}