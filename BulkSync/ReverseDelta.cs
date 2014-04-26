using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Diagnostics;

namespace BulkSync
{
    public class ReverseDelta: IDisposable
    {
        FileStream target;
        BinaryReader delta;
        BinaryWriter reverseDelta;

        public ReverseDelta(string targetPath, string deltaPath, string reverseDeltaPath)
        {
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new ArgumentException("targetPath");
            }

            target = File.OpenRead(targetPath);
            delta = new BinaryReader(string.IsNullOrEmpty(deltaPath) ? Console.OpenStandardInput() : File.OpenRead(deltaPath));
            reverseDelta = new BinaryWriter(string.IsNullOrEmpty(reverseDeltaPath) ? Console.OpenStandardOutput() : File.Create(reverseDeltaPath));
        }

        #region IDisposable implementation

        public void Dispose()
        {
            if (target != null)
            {
                target.Dispose();
            }

            if (reverseDelta != null)
            {
                reverseDelta.Dispose();
            }

            if (delta != null)
            {
                delta.Dispose();
            }
        }

        #endregion

        void WriteBlock(int skipBytes, int size, byte[] bytes)
        {
            if (skipBytes == 0 && size == 0)
            {
                return;
            }

            reverseDelta.Write(skipBytes);
            reverseDelta.Write(size);
            if (size != 0)
            {
                reverseDelta.Write(bytes, 0, size);
            }
        }

        public void Run()
        {
            byte[] hdrSig = delta.ReadBytes(MainClass.DELTA_HEADER.Length);
            if (!hdrSig.SequenceEqual(MainClass.DELTA_HEADER))
            {
                throw new InvalidDataException("Invalid signature");
            }

            reverseDelta.Write(MainClass.DELTA_HEADER);

            var buffer = new byte[MainClass.BLOCK_SIZE];

            while (target.Position < target.Length)
            {
                byte[] skipBytesBytes = delta.ReadBytes(4);
                if (skipBytesBytes.Length == 0)
                {
                    while (target.Position < target.Length)
                    {
                        long sizeRemaining = target.Length - target.Position;
                        int sizeRemainingInt32 = (int)Math.Min(sizeRemaining, (long)MainClass.BLOCK_SIZE);
                        var countRead = target.Read(buffer, 0, sizeRemainingInt32);
                        Debug.Assert(countRead == sizeRemainingInt32);
                        WriteBlock(0, sizeRemainingInt32, buffer);
                    }
                    break;
                }

                int skipBytes = BitConverter.ToInt32(skipBytesBytes, 0);
                var sizeBytes = delta.ReadInt32();
                if (sizeBytes > MainClass.BLOCK_SIZE)
                {
                    throw new InvalidDataException();
                }

                var c1 = delta.Read(buffer, 0, sizeBytes);
                if (c1 != sizeBytes)
                {
                    throw new InvalidDataException();
                }

                if (target.Position + skipBytes >= target.Length)
                {
                    var skipBytesToEnd = target.Length - target.Position;
                    Debug.Assert(skipBytesToEnd <= int.MaxValue);
                    skipBytes = (int)skipBytesToEnd;
                }

                target.Seek(skipBytes, SeekOrigin.Current);
                if (target.Position + sizeBytes > target.Length)
                {
                    var sizeBytesToEnd = target.Length - target.Position;
                    Debug.Assert(sizeBytesToEnd <= MainClass.BLOCK_SIZE);
                    sizeBytes = (int)sizeBytesToEnd;
                }

                var readSize = target.Read(buffer, 0, sizeBytes);
                Debug.Assert(readSize == sizeBytes);
                WriteBlock(skipBytes, sizeBytes, buffer);
            }
        }
    }
}

