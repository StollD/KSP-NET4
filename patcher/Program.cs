using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Octokit;

namespace KSPNET4.Patcher
{
    public class Program
    {
        /// <summary>
        /// The version of the patcher
        /// </summary>
        public const String Version = "1.4.0-2";
        
        /// <summary>
        /// The repository that we are referencing
        /// </summary>
        public const String User = "StollD";
        public const String Repo = "KSP-NET4";
        
        /// <summary>
        /// The entrypoint of the program
        /// </summary>
        public static async Task Main(String[] args)
        {
            Console.WriteLine($"KSP-NET4-Patcher: Version {Version}");
            Console.WriteLine();
            
            // Don't even try to run on MacOS at this time
            if (Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: MacOS is not supported in this version!");
                Console.ResetColor();

                Console.WriteLine();
                Console.Write("Press any key to continue...");
                Console.ReadKey();
                Environment.Exit(1);
            }
            
            // Connect to github
            GitHubClient client = new GitHubClient(new ProductHeaderValue("KSP-NET4-Patcher"));
            
            // Check if our version is up-to-date
            Release latest = await client.Repository.Release.GetLatest(User, Repo);
            if (Version != latest.TagName)
            {
#if !DEBUG
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"KSP-NET4-Patcher {latest.TagName} is available at https://github.com/{User}/{Repo}/releases/latest");
                Console.WriteLine($"You are currently running version {Version}");
                Console.WriteLine();
                Console.ResetColor();
#endif
            }
            
            // Get the actual release
            IReadOnlyList<Release> releases = await client.Repository.Release.GetAll(User, Repo);
            Release current = releases.FirstOrDefault(r => r.TagName == Version);
            if (current == null)
            {
                Environment.Exit(3);
            }
            
            // Check if this is running inside of a KSP directory
            if (!IsKspDirectory())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: You need to run KSP-NET4-Patcher from your KSP Installation Directory!");
                Console.ResetColor();

                Console.WriteLine();
                Console.Write("Press any key to continue...");
                Console.ReadKey();
                Environment.Exit(2);
            }
            
            // Check if this is already patched
            if (IsAlreadyPatched())
            {
                Console.WriteLine("This installation of KSP is already patched to use KSP-NET4.");
                if (Ask("Do you want to reinstall KSP-NET4?"))
                {
                    String dir = Directory.GetCurrentDirectory();
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        Console.WriteLine("Removing KSP_N4_Data/");
                        Directory.Delete(Path.Combine(dir, "KSP_N4_Data"), true);
                        Console.WriteLine("Removing KSP_N4_x64_Data/");
                        Directory.Delete(Path.Combine(dir, "KSP_N4_x64_Data"), true);
                        Console.WriteLine("Removing KSP_N4.exe");
                        File.Delete(Path.Combine(dir, "KSP_N4.exe"));
                        Console.WriteLine("Removing KSP_N4_x64.exe");
                        File.Delete(Path.Combine(dir, "KSP_N4_x64.exe"));
                    }

                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        Console.WriteLine("Removing KSP_N4_Data");
                        Directory.Delete(Path.Combine(dir, "KSP_N4_Data"), true);
                        Console.WriteLine("Removing KSP_N4.x86");
                        File.Delete(Path.Combine(dir, "KSP_N4.x86"));
                        Console.WriteLine("Removing KSP_N4.x86_64");
                        File.Delete(Path.Combine(dir, "KSP_N4.x86_64"));
                    }
                    
                    Console.WriteLine("Removing KSP bugfix plugin...");
                    File.Delete(Path.Combine(dir, "GameData", "KSP-NET4.dll"));
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            
            // Download the required data for this platform
            String assetUrl = current.Assets.First(r =>
                r.Name.StartsWith("NET4-Patch-" + Environment.OSVersion.Platform.ToString())).BrowserDownloadUrl;
            String filename = Path.GetTempFileName();

            // Download the file and save it to a temporary location
            Console.WriteLine("Downloading new mono runtime...");
            await new WebClient().DownloadFileTaskAsync(assetUrl, filename);

            // Copy the Unity Data folders
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                String dir = Directory.GetCurrentDirectory();
                if (Directory.Exists(Path.Combine(dir, "KSP_Data")))
                {
                    Console.WriteLine("Creating KSP_N4");
                    CopyAll("KSP_Data", "KSP_N4_Data");
                    File.Copy("KSP.exe", "KSP_N4.exe");
                }
                if (Directory.Exists(Path.Combine(dir, "KSP_x64_Data")))
                {
                    Console.WriteLine("Creating KSP_N4_x64");
                    CopyAll("KSP_x64_Data", "KSP_N4_x64_Data");
                    File.Copy("KSP_x64.exe", "KSP_N4_x64.exe");
                }
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                Console.WriteLine("Creating KSP_N4");
                CopyAll("KSP_Data", "KSP_N4_Data");
                File.Copy("KSP.x86", "KSP_N4.x86");
                File.Copy("KSP.x86_64", "KSP_N4.x86_64");
            }
            
            // Extract the downloaded runtime
            Console.WriteLine("Extracting the downloaded runtime...");
            ZipArchive archive = ZipFile.OpenRead(filename);
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                String completeFileName = Path.Combine(Directory.GetCurrentDirectory(), file.FullName);
                String directory = Path.GetDirectoryName(completeFileName);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (file.Name != "")
                {
                    Console.WriteLine($"Extracting {file.FullName}...");
                    file.ExtractToFile(completeFileName, true);
                }
            }
            
            // Download the bugfixing plugin
            Console.WriteLine("Downloading KSP bugfixing plugin...");
            filename = Path.GetTempFileName();
            await new WebClient().DownloadFileTaskAsync(
                current.Assets
                    .First(r => r.Name.StartsWith("KSP-NET4") && !r.Name.StartsWith("KSP-NET4-Patcher"))
                    .BrowserDownloadUrl, filename);
            archive = ZipFile.OpenRead(filename);
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                String completeFileName = Path.Combine(Directory.GetCurrentDirectory(), file.FullName);
                String directory = Path.GetDirectoryName(completeFileName);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (file.Name != "")
                {
                    Console.WriteLine($"Extracting {file.FullName}...");
                    file.ExtractToFile(completeFileName, true);
                }
            }
            
            // We are done
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Patching completed successfully!");
            Console.ResetColor();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.WriteLine(
                    "You can now run KSP using the new mono engine, by starting KSP_N4.exe or KSP_N4_x64.exe.");
                Console.WriteLine(
                    "If you want to remove the patch you just need to delete those two exe files, and their corresponding _Data folders.");
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                Console.WriteLine(
                    "You can now run KSP using the new mono engine, by starting KSP_N4.x86 or KSP_N4.x86_64.");
                Console.WriteLine(
                    "If you want to remove the patch you just need to delete those two executables, and the KSP_N4_Data folder.");
            }

            Console.WriteLine();
            Console.Write("Press any key to continue...");
            Console.ReadKey();
        }
        
        private static void CopyAll(String sourcePath, String destinationPath)
        {
            String[] directories = Directory.GetDirectories(sourcePath, "*.*", SearchOption.AllDirectories);

            foreach (String dirPath in directories)
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
            }

            String[] files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);

            foreach (String newPath in files)
            {
                File.Copy(newPath, newPath.Replace(sourcePath, destinationPath));
            }
        }

        public static Boolean Ask(String question)
        {
            Console.Write(question + " (Y/N): ");
            Char c = Console.ReadKey().KeyChar;
            Console.WriteLine();
            if (c == 'Y' || c == 'y')
            {
                return true;
            }

            if (c == 'N' || c == 'n')
            {
                return false;
            }

            return Ask(question);
        }

        public static Boolean IsKspDirectory()
        {
            String dir = Directory.GetCurrentDirectory();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return Directory.Exists(Path.Combine(dir, "KSP_Data")) ||
                       Directory.Exists(Path.Combine(dir, "KSP_x64_Data"));
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return Directory.Exists(Path.Combine(dir, "KSP_Data"));
            }

            return false;
        }
        
        public static Boolean IsAlreadyPatched()
        {
            String dir = Directory.GetCurrentDirectory();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return Directory.Exists(Path.Combine(dir, "KSP_N4_Data")) ||
                       Directory.Exists(Path.Combine(dir, "KSP_N4_x64_Data"));
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return Directory.Exists(Path.Combine(dir, "KSP_N4_Data"));
            }

            return false;
        }
    }
}