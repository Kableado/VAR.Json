using System;
using System.IO;
using System.Text;

namespace VAR.Json.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            // http://www.json.org/JSON_checker/

            string currentPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            currentPath = FindPath(currentPath, "tests");
            
            // Test all files
            string[] files;
            files = Directory.GetFiles(currentPath, "*.json");
            foreach (string file in files)
            {
                TestFile(file);
            }

            Console.Read();
        }

        private static void TestFile(string fileName)
        {
            string testName = Path.GetFileNameWithoutExtension(fileName);
            string fileContent = File.ReadAllText(fileName, Encoding.UTF8);
            if (testName.StartsWith("fail"))
            {
                TestFailCase(testName, fileContent);
            }
            if (testName.StartsWith("pass"))
            {
                TestPassCase(testName, fileContent);
            }
        }

        private static void TestFailCase(string testName, string fileContent)
        {
            JsonParser parser = new JsonParser();
            object result;
            try
            {
                result = parser.Parse(fileContent);
            }
            catch (Exception ex)
            {
                OutputFailure(testName, fileContent, ex);
                return;
            }
            if (parser.Tainted == false)
            {
                OutputFailure(testName, fileContent, result);
                return;
            }
            Console.Out.WriteLine("OK! {0}", testName);
        }

        private static void TestPassCase(string testName, string fileContent)
        {
            JsonParser parser = new JsonParser();
            object result;
            try
            {
                result = parser.Parse(fileContent);
            }
            catch (Exception ex)
            {
                OutputFailure(testName, fileContent, ex);
                return;
            }
            if (parser.Tainted)
            {
                OutputFailure(testName, fileContent, result);
                return;
            }
            Console.Out.WriteLine("OK! {0}", testName);
        }

        private static void OutputFailure(string testName, string fileContent, object obj)
        {
            Console.Out.WriteLine("Failure! {0}", testName);
            Console.Out.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.Out.WriteLine("Content:\n{0}", fileContent);
            Console.Out.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            if (obj is Exception)
            {
                Exception ex = obj as Exception;
                Console.Out.WriteLine("Ex.Message: {0}", ex.Message);
                Console.Out.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Console.Out.WriteLine("Ex.Stacktrace:\n{0}", ex.StackTrace);
                Console.Out.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }
            if (obj != null && (obj is Exception) == false)
            {
                JsonWriter writter = new JsonWriter(new JsonWriterConfiguration(indent: true));
                Console.Out.WriteLine("Parsed:\n{0}", writter.Write(obj));
                Console.Out.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }
        }

        private static string FindPath(string currentPath, string directory)
        {
            do
            {
                string testPath = Path.Combine(currentPath, directory);
                if (Directory.Exists(testPath))
                {
                    currentPath = testPath;
                    Console.Out.WriteLine(testPath);
                    break;
                }
                else
                {
                    DirectoryInfo dirInfo = Directory.GetParent(currentPath);
                    if (dirInfo == null)
                    {
                        throw new Exception(string.Format("FindPath: Directory {0} not found", directory));
                    }
                    currentPath = dirInfo.ToString();
                }
            } while (string.IsNullOrEmpty(currentPath) == false);
            return currentPath;
        }
    }
}
