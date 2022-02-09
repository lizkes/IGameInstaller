using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IGameInstaller.Model
{
    public class HookStream : Stream
    {
        public Stream UnderlayStream { get; set; }
        public long UnderlayPosition { get; set; }
        public long TotalBytes { get; set; }
        public int BeforeProgress { get; set; } = -1;
        public IProgress<(string, string, int)> Progress { get; set; }
        public CancellationToken CancelToken { get; set; }

        public HookStream(Stream stream, CancellationToken cancelToken)
        {
            UnderlayStream = stream;
            UnderlayPosition = 0;
            TotalBytes = 0;
            CancelToken = cancelToken;
        }
        public HookStream(Stream stream, long totalBytes, IProgress<(string, string, int)> progress, CancellationToken cancelToken)
        {
            UnderlayStream = stream;
            UnderlayPosition = 0;
            TotalBytes = totalBytes;
            Progress = progress;
            CancelToken = cancelToken;
        }

        public override bool CanRead => UnderlayStream.CanRead;

        public override bool CanSeek => UnderlayStream.CanSeek;

        public override bool CanWrite => UnderlayStream.CanWrite;

        public override long Length => UnderlayStream.Length;

        public override long Position { 
            get => UnderlayPosition; 
            set {
                UnderlayStream.Position = value;
                UnderlayPosition = value;
            } 
        }

        public override void Flush()
        {
            UnderlayStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (CancelToken.IsCancellationRequested) {
                return 0;
            }

            var readBytes = UnderlayStream.Read(buffer, offset, count);
            UnderlayPosition += readBytes;

            if (TotalBytes != 0)
            {
                var nowProgress = (int)Math.Round(UnderlayPosition / (double)TotalBytes * 100, 0);
                if (nowProgress > BeforeProgress)
                {
                    BeforeProgress = nowProgress;
                    Progress.Report(("keep", $"{nowProgress} %", nowProgress));
                }
            }

            return readBytes;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return UnderlayStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            UnderlayStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            UnderlayStream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            UnderlayStream.Close();
        }

        public override void WriteByte(byte value)
        {
            UnderlayPosition += 1;
            UnderlayStream.WriteByte(value);
        }
    }
}
