using System;

namespace IGameInstaller.IGameException
{
    public class DownloadExtractCanceledException: Exception
    {
        public DownloadExtractCanceledException() { }

        public DownloadExtractCanceledException(string message) : base(message) { }

        public DownloadExtractCanceledException(string message, Exception inner) : base(message, inner) { }
    }
}
