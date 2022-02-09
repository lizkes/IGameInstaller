using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace IGameInstaller.Helper
{
    public class ScriptHelper
    {
        public static void MoveFilesRecursively(string sourceDirPath, string destDirPath)
        {
            DirectoryInfo sourceDir = new(sourceDirPath);
            Directory.CreateDirectory(destDirPath);
            if (sourceDir.Exists)
            {
                foreach (DirectoryInfo dir in sourceDir.GetDirectories())
                {
                    string destSubDirPath = Path.Combine(destDirPath, dir.Name);
                    Directory.CreateDirectory(destSubDirPath);
                    MoveFilesRecursively(dir.FullName, destSubDirPath);
                }
                foreach (FileInfo file in sourceDir.GetFiles())
                {
                    string destSubFilePath = Path.Combine(destDirPath, file.Name);
                    if (File.Exists(destSubFilePath))
                    {
                        File.Delete(destSubFilePath);
                    }
                    file.MoveTo(destSubFilePath);
                }
            }
        }

        public static void CopyFile(string sourceFilePath, string destDirPath)
        {
            if (File.Exists(sourceFilePath))
            {
                string destDirFilePath = Path.Combine(destDirPath, Path.GetFileName(sourceFilePath));
                File.Copy(sourceFilePath, destDirFilePath, true);
            }
        }

        public static string GetInstallPath()
        {
            return App.InstallConfig.InstallPath;
        }

        public static string GetDocumentLocation()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public static string CombinePath(params string[] paths)
        {
            return Path.Combine(paths);
        }

        public static void DeleteAllBut(string destdirPath, string[] excludeFileNames = null, string[] excludeDirNames = null)
        {
            var distDirInfo = new DirectoryInfo(destdirPath);
            if (!distDirInfo.Exists) return;

            if (excludeFileNames == null)
            {
                excludeFileNames = new string[] {};
            }
            if (excludeDirNames == null)
            {
                excludeDirNames = new string[] {};
            }

            foreach (FileInfo fileInfo in distDirInfo.EnumerateFiles())
            {
                if (!excludeFileNames.Contains(fileInfo.Name))
                {
                    DeleteEvenWhenUsed(fileInfo.FullName);
                }
            }
            foreach (DirectoryInfo dirInfo in distDirInfo.EnumerateDirectories())
            {
                if (!excludeDirNames.Contains(dirInfo.Name))
                {
                    DeleteEvenWhenUsed(dirInfo.FullName);
                }
            }
        }
        public static void Retry(Action action, int retryNum = 15, int delay = 200)
        {
            for (int i = 0; i < retryNum; i++)
            {
                try
                {
                    action();
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(delay);
                    continue;
                }
            }
            action();
        }
        public static void DeleteEvenWhenUsed(string path, bool recursive = true)
        {
            if (Directory.Exists(path))
            {
                Retry(() => Directory.Delete(path, recursive));
            }
            else if (File.Exists(path))
            {
                Retry(() => File.Delete(path));
            }
        }
    }
}
