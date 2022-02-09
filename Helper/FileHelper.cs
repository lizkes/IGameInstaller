using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IGameInstaller.Helper
{
    public class FileHelper
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

        public static void CopyFilesRecursively(string sourceDirPath, string destDirPath)
        {
            DirectoryInfo sourceDir = new(sourceDirPath);
            Directory.CreateDirectory(destDirPath);
            if (sourceDir.Exists)
            {
                foreach (DirectoryInfo dir in sourceDir.GetDirectories())
                {
                    string destSubDirPath = Path.Combine(destDirPath, dir.Name);
                    Directory.CreateDirectory(destSubDirPath);
                    CopyFilesRecursively(dir.FullName, destSubDirPath);
                }
                foreach (FileInfo file in sourceDir.GetFiles())
                {
                    file.CopyTo(Path.Combine(destDirPath, file.Name), true);
                }
            }
        }

        public static string CreateRandomNameDir(string parentDirPath)
        {
            Directory.CreateDirectory(parentDirPath);

            string childDirPath = Path.Combine(parentDirPath, RandomHelper.GetFixLengthRandomString(6));
            while (Directory.Exists(childDirPath))
            {
                childDirPath = Path.Combine(parentDirPath, RandomHelper.GetFixLengthRandomString(6));
            }
            Directory.CreateDirectory(childDirPath);
            return childDirPath;
        }

        public static string GetMaxFreeSpaceDriver()
        {
            string driveName = @"C:\";
            long maxFreeSpace = 0;
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.DriveType == DriveType.Fixed && driveInfo.AvailableFreeSpace > maxFreeSpace)
                {
                    maxFreeSpace = driveInfo.AvailableFreeSpace;
                    driveName = driveInfo.Name;
                }
            }
            return driveName;
        }
        public static long GetDriverFreeSize(string driverName)
        {
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.DriveType == DriveType.Fixed && driverName == driveInfo.Name)
                {
                    return driveInfo.AvailableFreeSpace;
                }
            }
            return 0;
        }

        public static void Retry(Action action, int retryNum = 15, int delay = 200)
        {
            for (int i = 0; i < retryNum; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(delay);
                    continue;
                }
            }
            action();
        }

        public static async Task RetryAsync(Action action, int retryNum = 15, int delay = 200)
        {
            for (int i = 0; i < retryNum; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception)
                {
                    await Task.Delay(delay);
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

        public static async Task DeleteEvenWhenUsedAsync(string path, bool recursive = true)
        {
            if (Directory.Exists(path))
            {
                await RetryAsync(() => Directory.Delete(path, recursive));
            } 
            else if (File.Exists(path))
            {
                await RetryAsync(() => File.Delete(path));
            }
        }

        public static string SizeSuffix(long value, int decimalPlaces = 2)
        {
            string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} B", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }
}
