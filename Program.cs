using System;
using System.IO;
using DiscUtils;
using DiscUtils.Iso9660;
using System.Text.RegularExpressions;

namespace Q9xS
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    throw new ArgumentException("One of your arguments are missing\n" +
                        "The correct syntax is: dotnet Q9xS updatePath isoPath");
                }

                Program self = new Program();
                bool extract = true;
                string updatePath = args[0];
                string isoPath = args[1];
                string extractedIsoPath = Path.GetFileNameWithoutExtension(isoPath);

                if (!Directory.Exists(updatePath))
                    throw new FileNotFoundException(updatePath + " does not exist");

                if (!File.Exists(isoPath))
                    throw new FileNotFoundException(isoPath + " does not exist");

                if (Directory.Exists(extractedIsoPath))
                {
                    Console.WriteLine(@"It looks like you've already extracted the iso. Would you like to re-extract? [y/any other key]");
                    string response = Console.ReadLine();

                    if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                        extract = false;
                }

                if (extract)
                    self.ExtractISO(isoPath, extractedIsoPath);

                self.Update9xDir(updatePath, extractedIsoPath);
                self.CreateISO(extractedIsoPath);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        public void Update9xDir(string updatesDir, string extracted9xDir) {
            bool updated = false;

            string subDir = null;
            if (Directory.Exists(extracted9xDir+@"\Win95"))
                subDir = extracted9xDir + @"\Win95";
            else if (Directory.Exists(extracted9xDir+@"\Win98"))
                subDir = extracted9xDir + @"\Win98";
            else if (Directory.Exists(extracted9xDir+@"\Win9x"))
                subDir = extracted9xDir + @"\Win9x";
            else
            {
                throw new DirectoryNotFoundException(extracted9xDir + " is not an extracted windows iso");
            }

            string subDirName = new DirectoryInfo(subDir).Name;
            string layoutPath = subDir + @"\layout.inf";
            string layout1Path = subDir + @"\layout1.inf";
            string layout2Path = subDir + @"\layout2.inf";

            CopyFreshLayoutInf(layoutPath, subDirName);

            if (File.Exists(@"layouts\" + subDirName + @"\layout1.inf"))
                CopyFreshLayoutInf(layout1Path, subDirName);

            if (File.Exists(@"layouts\" + subDirName + @"\layout2.inf"))
                CopyFreshLayoutInf(layout2Path, subDirName);

            foreach (string update in Directory.GetFiles(updatesDir))
            {
                string copyTo = subDir + @"\" + Path.GetFileName(update);
                FileInfo updateInfo = new FileInfo(update);
                FileInfo copyToInfo = new FileInfo(copyTo);
                if (!File.Exists(copyTo) || updateInfo.LastWriteTime > copyToInfo.LastWriteTime)
                {
                    updated = true;
                    updateInfo.Attributes = FileAttributes.Normal;
                    updateInfo.CopyTo(copyToInfo.FullName, true);
                    copyToInfo.Attributes = FileAttributes.Normal;
                    Console.WriteLine(updateInfo.FullName + " was copied to " + copyToInfo.FullName);
                }
            }

            // begin updating layout(s)
            if (updated)
            {
                string[] updatedSubDirFiles = Directory.GetFiles(subDir);
                UpdateLayout(updatedSubDirFiles, layoutPath);

                if (File.Exists(layout1Path))
                    UpdateLayout(updatedSubDirFiles, layout1Path);

                if (File.Exists(layout2Path))
                    UpdateLayout(updatedSubDirFiles, layout2Path);
            }
        }

        public void CopyFreshLayoutInf(string layoutPath, string subDirName)
        {
            if (!File.Exists(layoutPath))
            {
                string layoutCopyFromPath = @"layouts\" + subDirName + @"\" + Path.GetFileName(layoutPath);
                if (!File.Exists(layoutCopyFromPath))
                {
                    throw new FileNotFoundException(layoutCopyFromPath + " does not exist.\n" +
                        "Make sure that you have the layouts folder in the same directory" +
                        "as this application.");
                }
                Console.WriteLine(layoutPath + " does not exist, copying a fresh one...");
                File.Copy(layoutCopyFromPath, layoutPath);
            }
        }

        public void UpdateLayout(string[] updatedSubDirFiles, string layoutToUpdatePath)
        {
            string layoutText = File.ReadAllText(layoutToUpdatePath);

            foreach (string file in updatedSubDirFiles)
            {
                string fileName = Path.GetFileName(file);
                long fileSize = new FileInfo(file).Length;
                for(int i = 0; i < 29; i++)
                {
                    string regEx = fileName + @"=" + i + @",,[1-9]*";
                    Match match = Regex.Match(layoutText, regEx, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        layoutText = Regex.Replace(layoutText, regEx, fileName + "=" + i +",," + fileSize, RegexOptions.IgnoreCase);
                        break;
                    }
                }
                Console.WriteLine("Layout entry for " + fileName + " updated.");
            }

            File.WriteAllText(layoutToUpdatePath, layoutText);
            Console.WriteLine(layoutToUpdatePath + " successfully updated.");
        }

        public CDBuilder AddToIso(CDBuilder builder, string dirToIso)
        {
            foreach (string directory in Directory.GetDirectories(dirToIso))
            {
                AddToIso(builder, directory);
            }
            foreach (string file in Directory.GetFiles(dirToIso))
            {
                Stream file2add = File.Open(file, FileMode.Open);
                string file2addPath = file.Substring(file.IndexOf(@"\", 1) + 1);
                builder.AddFile(file2addPath, file2add);
                Console.WriteLine(file2addPath + " was added to the iso.");
            }
            return builder;
        }

        public void CreateISO(string dirToIso)
        {
            CDBuilder builder = new CDBuilder
            {
                UseJoliet = true,
                VolumeIdentifier = "WIN_9X"
            };

            builder = AddToIso(builder, dirToIso);
            builder.Build(Path.GetFileNameWithoutExtension(dirToIso)+"_slip.iso");
            Console.WriteLine(Path.GetFileNameWithoutExtension(dirToIso+".iso successfully updated."));
        }

        public void ExtractISO(string toExtract, string folderName)
        {
            CDReader Reader = new CDReader(File.Open(toExtract, FileMode.Open), true);
            ExtractDirectory(Reader.Root, folderName + "\\", "");
            Console.WriteLine(toExtract + " was succesfully extracted.");
            Reader.Dispose();
        }

        public void ExtractDirectory(DiscDirectoryInfo Dinfo, string RootPath, string PathinISO)
        {
            if (!string.IsNullOrWhiteSpace(PathinISO))
            {
                PathinISO += "\\" + Dinfo.Name;
            }
            RootPath += "\\" + Dinfo.Name;
            AppendDirectory(RootPath);
            foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
            {
                ExtractDirectory(dinfo, RootPath, PathinISO);
            }
            foreach (DiscFileInfo finfo in Dinfo.GetFiles())
            {
                using (Stream FileStr = finfo.OpenRead())
                {
                    using (FileStream Fs = File.Create(RootPath + "\\" + finfo.Name))
                    {
                        FileStr.CopyTo(Fs, 4 * 1024);
                        Console.WriteLine(finfo.Name + " was extracted.");
                    }
                }
            }
        }

        static void AppendDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (DirectoryNotFoundException)
            {
                AppendDirectory(Path.GetDirectoryName(path));
            }
            catch (PathTooLongException)
            {
                AppendDirectory(Path.GetDirectoryName(path));
            }
        }
    }
}
