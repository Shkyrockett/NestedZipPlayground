// <copyright file="Program.cs" company="Shkyrockett" >
// Copyright © 2024 Shkyrockett. All rights reserved.
// </copyright>
// <author id="shkyrockett">Shkyrockett</author>
// <license>
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </license>
// <summary></summary>
// <remarks></remarks>

using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;

namespace NestedZipPlayground;

/// <summary>
/// 
/// </summary>
internal class Program
{
    /// <summary>
    /// Defines the entry point of the application.
    /// </summary>
    private static void Main()
    {
        string testText = "This is some repeated text to use for compressing into files to test the size of the resulting compressed files.";
        const int Files = 25;
        const int Lines = 25;
        string rootFolder = @"C:\Temp";
        string testFilesFolder = @$"{rootFolder}\TestFiles";
        string directCompressedZipFilePath = @$"{rootFolder}\direct_compressed.zip";
        string nonCompressedZipFilePath = @$"{rootFolder}\non_compressed.zip";
        string compressedNonCompressedZipFilePath = @$"{rootFolder}\compressed_non_compressed.zip";

        bool rootExists = Directory.Exists(rootFolder);

        // Create test files.
        Console.WriteLine($"Creating {Files + 1} text files with {25 + 1} lines of the text: \"{testText}\"");
        WriteTestFiles(testFilesFolder, Files, Lines, testText);
        Console.WriteLine("Test files created successfully.");

        // Direct compression.
        CreateStandardZip(testFilesFolder, directCompressedZipFilePath);
        Console.WriteLine("Direct compression completed.");

        // Non-compressed zip first, then compress.
        CreateNestedZip(testFilesFolder, nonCompressedZipFilePath, compressedNonCompressedZipFilePath);
        Console.WriteLine("Non-compressed zip first, then compressed completed.");

        // Compare sizes.
        Console.WriteLine();
        CompareResults(testFilesFolder, directCompressedZipFilePath, nonCompressedZipFilePath, compressedNonCompressedZipFilePath);
        Console.WriteLine();

        // Wait for user input before cleanup.
        Console.WriteLine("Press any key to clean up...");
        Console.ReadKey();

        // Cleanup.
        if (rootExists)
        {
            CleanupResults(testFilesFolder, directCompressedZipFilePath, nonCompressedZipFilePath, compressedNonCompressedZipFilePath);
        }
        else
        {
            DeleteDirectory(rootFolder);
        }

        Console.WriteLine("Cleanup completed.");
    }

    /// <summary>
    /// Writes the test files.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    /// <param name="files">The files.</param>
    /// <param name="lines">The lines.</param>
    /// <param name="text">The text.</param>
    private static void WriteTestFiles(string directoryPath, int files, int lines, string text)
    {
        if (Directory.Exists(directoryPath))
        {
            // Clean up the folder if the previous run didn't.
            CleanDirectory(directoryPath);
        }
        else
        {
            // Set up the folder to write to.
            Directory.CreateDirectory(directoryPath);
        }

        // Create files with decent amounts of redundency.
        // Files with lots of redundency compress better than random files.
        for (int i = 0; i <= files; i++)
        {
            string filePath = Path.Combine(directoryPath, $"file{i}.txt");
            using StreamWriter writer = new(filePath, false, Encoding.UTF8);
            for (int j = 0; j <= lines; j++)
            {
                writer.WriteLine(text);
            }
        }
    }

    /// <summary>
    /// Creates the standard zip.
    /// </summary>
    /// <param name="testFilesFolder">The source directory.</param>
    /// <param name="zipFilePath">The zip file path.</param>
    private static void CreateStandardZip(string testFilesFolder, string zipFilePath)
    {
        if (File.Exists(zipFilePath))
        {
            // Delete the file if the previous run didn't clean up.
            File.Delete(zipFilePath);
        }

        // When files are added to a zip archive, the files are individually compressed then appended to the zip file.
        // The files are not compressed in relation to the other files in the archive. 
        ZipFile.CreateFromDirectory(testFilesFolder, zipFilePath, CompressionLevel.SmallestSize, false);
    }

    /// <summary>
    /// Creates the nested zip.
    /// </summary>
    /// <param name="sourceDirectory">The source directory.</param>
    /// <param name="nonCompressedZipPath">The non compressed zip path.</param>
    /// <param name="compressedZipPath">The compressed zip path.</param>
    private static void CreateNestedZip(string sourceDirectory, string nonCompressedZipPath, string compressedZipPath)
    {
        if (File.Exists(nonCompressedZipPath))
        {
            // Delete the file if the previous run didn't clean up.
            File.Delete(nonCompressedZipPath);
        }

        if (File.Exists(compressedZipPath))
        {
            // Delete the file if the previous run didn't clean up.
            File.Delete(compressedZipPath);
        }

        // Create a non-compressed zip file to maximize the redundency in the combined file.
        using (FileStream zipToOpen = new(nonCompressedZipPath, FileMode.Create))
        {
            using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Create);
            foreach (string file in Directory.GetFiles(sourceDirectory))
            {
                archive.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.NoCompression);
            }
        }

        // Compress the non-compressed zip file.
        // By compressing the non-compressed file, the individual files are concatenated as a single file, meaning compression can now happen across logical files.
        using FileStream originalFileStream = new(nonCompressedZipPath, FileMode.Open);
        using FileStream compressedFileStream = new(compressedZipPath, FileMode.Create);
        using GZipStream compressionStream = new(compressedFileStream, CompressionLevel.SmallestSize);
        originalFileStream.CopyTo(compressionStream);
    }

    /// <summary>
    /// Compares the results.
    /// </summary>
    /// <param name="testFilesFolderPath">Path to your test files directory.</param>
    /// <param name="directlyCompressedZipPath">The direct compression path.</param>
    /// <param name="nonCompressedZipFilePath"></param>
    /// <param name="compressedNonCompressedZipPath">The compressed non compressed path.</param>
    private static void CompareResults(string testFilesFolderPath, string directlyCompressedZipPath, string nonCompressedZipFilePath, string compressedNonCompressedZipPath)
    {
        // Get the file sizes.
        long testFilesDirectorySize = GetDirectorySize(testFilesFolderPath, true);
        long directlyCompressedZipFileSize = new FileInfo(directlyCompressedZipPath).Length;
        long nonCompressedZipFileSize = new FileInfo(nonCompressedZipFilePath).Length;
        long compressedNonCompressedZipFileSize = new FileInfo(compressedNonCompressedZipPath).Length;

        // Print the file sizes.
        Console.WriteLine($"Total size of uncompressed files:\t\t{testFilesDirectorySize,5} bytes.");
        Console.ForegroundColor = (directlyCompressedZipFileSize <= compressedNonCompressedZipFileSize) ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"Directly compressed zip file size:\t\t{directlyCompressedZipFileSize,5} bytes.");
        Console.ResetColor();
        Console.WriteLine($"Non-compressed zip file size:\t\t\t{nonCompressedZipFileSize,5} bytes.");
        Console.ForegroundColor = (directlyCompressedZipFileSize >= compressedNonCompressedZipFileSize) ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"Compressed nested non-compressed zip file size:\t{compressedNonCompressedZipFileSize,5} bytes.");
        Console.ResetColor();

        // Compare the sizes.
        if (directlyCompressedZipFileSize < compressedNonCompressedZipFileSize)
        {
            Console.WriteLine("Direct compression resulted in a smaller file.");
        }
        else if (directlyCompressedZipFileSize > compressedNonCompressedZipFileSize)
        {
            Console.WriteLine("Compressing the non-compressed zip file resulted in a smaller file.");
        }
        else
        {
            Console.WriteLine("Both methods resulted in files of the same size.");
        }
    }

    /// <summary>
    /// Gets the size of the contents of a directory.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    /// <param name="recurse">If set to <c>true</c> recursively capture sub directories.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)] // Aggressive inlining to try to avoid stack overflows from the recursive calls.
    private static long GetDirectorySize(string directoryPath, bool recurse = false)
    {
        long totalSize = 0;
        DirectoryInfo directoryInfo = new(directoryPath);

        // Get the size of all files in the directory.
        FileInfo[] files = directoryInfo.GetFiles();
        foreach (FileInfo file in files)
        {
            totalSize += file.Length;
        }

        if (recurse)
        {
            // Recursively get the size of all files in subdirectories.
            DirectoryInfo[] subDirectories = directoryInfo.GetDirectories();
            foreach (DirectoryInfo subDirectory in subDirectories)
            {
                totalSize += GetDirectorySize(subDirectory.FullName, recurse);
            }
        }

        return totalSize;
    }

    /// <summary>
    /// Cleanups the results.
    /// </summary>
    /// <param name="testFilesFolder">The test files folder.</param>
    /// <param name="directCompressedZipFilePath">The direct compressed zip file path.</param>
    /// <param name="nonCompressedZipFilePath">The non compressed zip file path.</param>
    /// <param name="compressedNonCompressedZipFilePath">The compressed non compressed zip file path.</param>
    private static void CleanupResults(string testFilesFolder, string directCompressedZipFilePath, string nonCompressedZipFilePath, string compressedNonCompressedZipFilePath)
    {
        // Cleanup test files.
        DeleteDirectory(testFilesFolder);

        // Cleanup zip files.
        File.Delete(directCompressedZipFilePath);
        File.Delete(nonCompressedZipFilePath);
        File.Delete(compressedNonCompressedZipFilePath);
    }

    /// <summary>
    /// Cleanups the files and sub-directories out of a directory, but keeps the directory.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    private static void CleanDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            foreach (string file in Directory.GetFiles(directoryPath))
            {
                File.Delete(file);
            }

            foreach (string subDirectory in Directory.GetDirectories(directoryPath))
            {
                Directory.Delete(subDirectory, true);
            }
        }
    }

    /// <summary>
    /// Delete the directory, files and sub-directories.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    private static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            foreach (string file in Directory.GetFiles(directoryPath))
            {
                File.Delete(file);
            }

            foreach (string subDirectory in Directory.GetDirectories(directoryPath))
            {
                Directory.Delete(subDirectory, true);
            }

            Directory.Delete(directoryPath, true);
        }
    }
}
