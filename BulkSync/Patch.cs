using System;
using System.IO;
using System.Linq;

namespace BulkSync
{
    public class Patch : IDisposable
    {
        FileStream target;
        BinaryReader delta;

        public Patch(string deltaPath, string targetPath)
        {
            target = File.OpenWrite(targetPath);
            delta = new BinaryReader(string.IsNullOrEmpty(deltaPath) ? Console.OpenStandardInput() : File.OpenRead(deltaPath));
        }

        #region IDisposable implementation

        public void Dispose()
        {
            if (target != null)
            {
                target.Dispose();
            }

            if (delta != null)
            {
                delta.Dispose();
            }
        }

        #endregion

        public void Run()
        {
            byte[] hdrSig = delta.ReadBytes(MainClass.DELTA_HEADER.Length);
            if (!hdrSig.SequenceEqual(MainClass.DELTA_HEADER))
            {
                throw new InvalidDataException("Invalid signature");
            }

            var buffer = new byte[MainClass.BLOCK_SIZE];

            while (true)
            {
                byte[] skipBytesBytes = delta.ReadBytes(4);
                if (skipBytesBytes.Length == 0)
                {
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

                target.Seek(skipBytes, SeekOrigin.Current);
                if (sizeBytes != 0)
                {
                    target.Write(buffer, 0, sizeBytes);
                }
            }

            target.SetLength(target.Position);
        }
    }
}

