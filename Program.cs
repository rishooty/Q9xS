using System;
using System.IO;
using DiscUtils;
using DiscUtils.Iso9660;

namespace Q9xS
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("One of your arguments are missing");
                Console.WriteLine("The correct syntax is: dotnet Q9xS updatePath isoPath bootImagePath");
                Environment.Exit(0);
            }

            Program self = new Program();
            bool extract = true;
            string updatePath = args[0];
            string isoPath = args[1];
            string bootImage = args[2];
            string extractedIsoPath = Path.GetFileNameWithoutExtension(isoPath);

            if (Directory.Exists(extractedIsoPath))
            {
                Console.WriteLine(@"It looks like you've already extracted the iso. Would you like to re-extract? [y/any other key]");
                string response = Console.ReadLine();

                if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                    extract = false;
            }

            if(extract)
                self.ExtractISO(isoPath, extractedIsoPath);
            self.Update9xDir(updatePath, extractedIsoPath);
            self.CreateISO(extractedIsoPath, bootImage);
        }

        public void Update9xDir(string updatesDir, string extracted9xDir) {
            string subDir = null;
            if (Directory.Exists(extracted9xDir+@"\Win95"))
                subDir = extracted9xDir + @"\Win95";
            else if (Directory.Exists(extracted9xDir+@"\Win98"))
                subDir = extracted9xDir + @"\Win98";
            else if (Directory.Exists(extracted9xDir+@"\Win9x"))
                subDir = extracted9xDir + @"\Win9x";
            else
            {
                Console.WriteLine(extracted9xDir + " is not an extracted windows iso");
                Environment.Exit(0);
            }

            foreach(string update in Directory.GetFiles(updatesDir))
            {
                string copyTo = subDir + @"\" + Path.GetFileName(update);
                FileInfo updateInfo = new FileInfo(update);
                FileInfo copyToInfo = new FileInfo(copyTo);
                if (!File.Exists(copyTo) || updateInfo.LastWriteTime > copyToInfo.LastWriteTime)
                {
                    updateInfo.Attributes = FileAttributes.Normal;
                    copyToInfo.Attributes = FileAttributes.Normal;
                    updateInfo.CopyTo(copyToInfo.FullName, true);
                    Console.WriteLine(updateInfo.FullName + " was copied to " + copyToInfo.FullName);
                }
            }
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

        public void CreateISO(string dirToIso, string bootImagePath)
        {
            CDBuilder builder = new CDBuilder();
            builder.UseJoliet = true;
            builder.VolumeIdentifier = "WIN_9X";

            if (!File.Exists(bootImagePath))
            {
                Console.WriteLine("Boot image does not exist, exiting.");
                Environment.Exit(0);
            }

            Stream boot = File.Open(bootImagePath, FileMode.Open);
            builder.SetBootImage(boot, BootDeviceEmulation.Diskette1440KiB, 0);

            builder = AddToIso(builder, dirToIso);

            DirectoryInfo bootDir = new DirectoryInfo(dirToIso + @"\[BOOT]");
            if (bootDir.Exists)
            {
                bootDir.Attributes = FileAttributes.Normal;
                bootDir.Delete();
            }
                
            builder.Build(Path.GetFileNameWithoutExtension(dirToIso)+".iso");
            Console.WriteLine(Path.GetFileNameWithoutExtension(dirToIso+".iso succesfully updated."));
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
            catch (DirectoryNotFoundException Ex)
            {
                AppendDirectory(Path.GetDirectoryName(path));
            }
            catch (PathTooLongException Ex)
            {
                AppendDirectory(Path.GetDirectoryName(path));
            }
        }

    }
}
