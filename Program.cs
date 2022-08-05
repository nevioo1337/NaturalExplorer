using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace WWS_Explorer_Console
{
    internal class Program
    {
        static string outFilePath = "output.html";
        static string templatePath = "template.html";
        static string deadDumpPath = "dead_dump.txt";

        static string rootPath = "natural_files\\";
        static string rootFile = "rootFile.NSN";

        static string[] fileArray = { };

        static string placeHolder = "{CONTENT}";

        static string template = "";
        static string content = "";

        static string GetLine(string fileName, int line)
        {
            using (var sr = new StreamReader(fileName, Encoding.GetEncoding("iso-8859-1")))
            {
                for (int i = 1; i < line; i++)
                    sr.ReadLine();
                return sr.ReadLine();
            }
        }

        static string GetFileType(string name)
        {
            foreach (var item in fileArray)
            {
                if (item.ToUpper().Contains(name.ToUpper()))
                {
                    return item.Replace(name, "");
                }
            }
            return "";
        }

        static string ExtractCallFromString(string rawData)
        {
            if (rawData.Contains("fetch return") || rawData.Contains("FETCH") || rawData.Contains("callnat"))
            {
                var reg = new Regex("'.*?'");
                var matches = reg.Matches(rawData);

                foreach (var item in matches)
                {
                    string extractedName = item.ToString().Replace("'", "");
                    return extractedName + GetFileType(extractedName);
                }
            }
            else if (rawData.Contains("parameter USING"))
            {
                int startIndex = rawData.IndexOf("parameter USING");
                rawData = rawData.Remove(0, startIndex);
                var extractedWords = rawData.Split(' ');

                for (int i = 2; i < extractedWords.Count(); i++)
                {
                    if (extractedWords[i] != " " && extractedWords[i] != "")
                    {
                        return extractedWords[i] + GetFileType(extractedWords[i]);
                    }
                }
            }
            else if (rawData.Contains("local using"))
            {
                int startIndex = rawData.IndexOf("local using");
                rawData = rawData.Remove(0, startIndex);
                var extractedWords = rawData.Split(' ');

                for (int i = 2; i < extractedWords.Count(); i++)
                {
                    if (extractedWords[i] != " " && extractedWords[i] != "")
                    {
                        return extractedWords[i] + GetFileType(extractedWords[i]);
                    }
                }
            }
            return "";
        }

        static void WriteContentToFile()
        {
            content = template.Replace(placeHolder, content);
            File.AppendAllText(outFilePath, content);
        }

        static bool DoesFileExistInArray_CI(string file)
        {
            for (int i = 0; i < fileArray.Count(); i++)
            {
                if (fileArray[i].ToUpper() == file.ToUpper())
                {
                    return true;
                }
            }
            return false;
        }

        static void StructureGenerator(string file)
        {
            try
            {
                int lineCount = File.ReadAllLines(rootPath + file).Count() + 1;
                for (int i = 1; i < lineCount; i++)
                {
                    string currLine = GetLine(rootPath + file, i);
                    string call = ExtractCallFromString(currLine);

                    if (call != String.Empty)
                    {
                        // DISPLAYS CALL
                        string guid = Guid.NewGuid().ToString();
                        if (!DoesFileExistInArray_CI(call))
                        {
                            // FILE DOES NOT EXIST
                            content += $"<div style=\"color: rgb(138, 14, 14); \"\"margin-bottom:5px;\"><div style=\"float:left; line-height:18px;\" onclick=\"toggleDiv('{guid}');\" id=\"{guid}_icon\">➕</div>{currLine}</div>\n";
                        }
                        else if (call == file)
                        {
                            // DIRECT RECURSION
                            content += $"<div style=\"color: rgb(14, 18, 138); \"\"margin-bottom:5px;\"><div style=\"float:left; line-height:18px;\" onclick=\"toggleDiv('{guid}');\" id=\"{guid}_icon\">➕</div>{currLine}</div>\n";
                        }
                        else
                        {
                            // NORMAL
                            content += $"<div style=\"color: rgb(9, 110, 15); \"\"margin-bottom:5px;\"><div style=\"float:left; line-height:18px;\" onclick=\"toggleDiv('{guid}');\" id=\"{guid}_icon\">➕</div>{currLine}</div>\n";
                        }

                        if (call != file)
                        {
                            // DIRECT ANTI RECURSION
                            content += $"<div id=\"{guid}\" style=\"margin-left:30px; display:none; overflow:hidden;\">\n";

                            Console.WriteLine(file + " -> " + call);
                            StructureGenerator(call);
                            content += "</div>\n";
                        }
                    }
                    else
                    {
                        // DISPLAYS SRC
                        content += $"<p style=\"margin-bottom:0; margin : 0; padding-top:0; color: rgb(255, 255, 255)\">{currLine}</p>";
                    }
                }
            }
            catch (Exception) { }
        }

        static void GetDeadFiles()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Status: Processing");
            Console.ForegroundColor = ConsoleColor.Gray;

            File.Delete(deadDumpPath);

            List<string> calls = new List<string>();

            fileArray = Directory.GetFiles(rootPath, "*.*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < fileArray.Count(); i++)
            {
                fileArray[i] = Path.GetFileName(fileArray[i]);
            }

            for (int i = 0; i < fileArray.Count(); i++)
            {
                Console.Title = $"Progress ({i + 1}/{fileArray.Count()})";

                int lineCount = File.ReadAllLines(rootPath + fileArray[i]).Count() + 1;
                for (int a = 1; a < lineCount; a++)
                {
                    string currLine = GetLine(rootPath + fileArray[i], a);
                    string call = ExtractCallFromString(currLine);

                    if (call != String.Empty)
                    {
                        calls.Add(call);
                        //Console.WriteLine($"{fileArray[i]} -> {call} [{i}]");
                    }
                }
            }

            var callsNoDupes = calls.Distinct().ToList();
            for (int i = 0; i < fileArray.Count(); i++)
            {
                bool callExists = false;
                for (int a = 0; a < callsNoDupes.Count(); a++)
                {
                    if (callsNoDupes[a].ToUpper() == fileArray[i].ToUpper())
                    {
                        callExists = true;
                    }
                }

                if (!callExists)
                {
                    Console.WriteLine(fileArray[i]);
                    File.AppendAllText(deadDumpPath, fileArray[i] + "\n");
                }
            }
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Status: Finished");
            Console.ReadLine();
        }

        static void CreateTree()
        {
            Console.Clear();

            fileArray = Directory.GetFiles(rootPath, "*.*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < fileArray.Count(); i++)
            {
                fileArray[i] = Path.GetFileName(fileArray[i]);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Status: Generating");
            Console.ForegroundColor = ConsoleColor.Magenta;

            File.Delete(outFilePath);
            template = File.ReadAllText(templatePath);

            StructureGenerator(rootFile);
            WriteContentToFile();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Status: Finished");
            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Title = "Natural Explorer";
            
            Console.WriteLine("[1] Generate Tree");
            Console.WriteLine("[2] Get Dead Files");
            string userInput = Console.ReadLine();
            if (userInput == "1")
            {
                Console.Clear();
                Console.WriteLine("Enter root file: ");
                rootFile = Console.ReadLine();
                CreateTree();
            }
            else if (userInput == "2")
            {
                Console.Clear();
                Console.WriteLine("Enter root file: ");
                rootFile = Console.ReadLine();
                GetDeadFiles();
            }
            else
            {
                Console.Clear();
                Console.WriteLine("Invalid input");
                Thread.Sleep(1000);
            }
        }
    }
}
