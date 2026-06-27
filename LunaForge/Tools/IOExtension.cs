using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LunaForge.Tools;

public static class DirectoryExtension
{
    extension(Directory)
    {
        /// <summary>
        /// Copy all files and subdirectories of a directory to another location.
        /// If the target directory doesn't exist, it will be created.
        /// If it already exists, files with the same name will be overwritten.
        /// </summary>
        /// <param name="sourceDirectory">Source directory</param>
        /// <param name="targetDirectory">Destination directory</param>
        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo dirSource = new(sourceDirectory);
            DirectoryInfo dirTarget = new(targetDirectory);

            CopyAll(dirSource, dirTarget);
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo dest)
        {
            Directory.CreateDirectory(dest.FullName);

            foreach (FileInfo fi in source.GetFiles())
                fi.CopyTo(Path.Combine(dest.FullName, fi.Name), true);

            foreach (DirectoryInfo dirSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextDestSubDir = dest.CreateSubdirectory(dirSourceSubDir.Name);
                CopyAll(dirSourceSubDir, nextDestSubDir);
            }
        }
    }
}

public static partial class FileExtension
{
    [LibraryImport("kernel32.dll", EntryPoint = "DeleteFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteFile(string lpFileName);

    extension(File)
    {
        /// <summary>
        /// Unlocks a DLL by deleting the MOTW.
        /// </summary>
        /// <param name="filePath">Full path to DLL.</param>
        /// <returns>True if the unlocking was sucessful. False if the file wasn't locked.</returns>
        /// <exception cref="FileNotFoundException">File is not found.</exception>
        public static bool UnblockDll(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Specified file doesn't exist.", filePath);

            string zoneId = $"{filePath}:Zone.Identifier";
            return DeleteFile(zoneId);
        }
    }
}