using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BulkSync
{
    public class Delta: IDisposable
    {
        Stream input;
        BinaryReader signature;
        BinaryWriter delta;

        public Delta(string inputPath, string signaturePath, string deltaPath)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentException("inputPath");
            }

            input = File.OpenRead(inputPath);
            signature = new BinaryReader(string.IsNullOrEmpty(signaturePath) ? Console.OpenStandardInput() : File.OpenRead(signaturePath));
            delta = new BinaryWriter(string.IsNullOrEmpty(deltaPath) ? Console.OpenStandardOutput() : File.Create(deltaPath));
        }

        #region IDisposable implementation

        public void Dispose()
        {
            if (input != null)
            {
                input.Dispose();
            }

            if (signature != null)
            {
                signature.Dispose();
            }

            if (delta != null)
            {
                delta.Dispose();
            }
        }

        #endregion

        int sigBlockLength;
        byte[] sigBlockHash;
        byte[] sigUndoBuffer;

        void InitSig()
        {
            byte[] hdrSig = signature.ReadBytes(MainClass.SIG_HEADER.Length);
            if (!hdrSig.SequenceEqual(MainClass.SIG_HEADER))
            {
                throw new InvalidDataException("Invalid signature");
            }
            sigUndoBuffer = signature.ReadBytes(MainClass.HASH_SIZE);
        }

        void ReadSig()
        {
            if (sigUndoBuffer == null)
            {
                sigBlockLength = 0;
                sigBlockHash = null;
                return;
            }

            if (sigUndoBuffer.Length != MainClass.HASH_SIZE)
            {
                throw new InvalidDataException("invalid data");
            }

            sigBlockHash = sigUndoBuffer;

            sigUndoBuffer = signature.ReadBytes(MainClass.HASH_SIZE);
            if (sigUndoBuffer.Length == MainClass.HASH_SIZE)
            {
                sigBlockLength = MainClass.BLOCK_SIZE;
            }
            else if (sigUndoBuffer.Length == 4)
            {
                sigBlockLength = BitConverter.ToInt32(sigUndoBuffer, 0);
                if (sigBlockLength >= MainClass.BLOCK_SIZE)
                {
                    throw new InvalidDataException("invalid length");
                }
                sigUndoBuffer = null;
            }
            else
            {
                throw new InvalidDataException("invalid data");
            }
        }

        void WriteBlock(int skipBytes, int size, byte[] bytes)
        {
            if (skipBytes == 0 && size == 0)
            {
                return;
            }

            delta.Write(skipBytes);
            delta.Write(size);
            if (size != 0)
            {
                delta.Write(bytes, 0, size);
            }
        }

        public void Run()
        {
            InitSig();

            delta.Write(MainClass.DELTA_HEADER);

            var buffer = new byte[MainClass.BLOCK_SIZE];
            using (var sha1 = new SHA1Managed())
            {
                int skipBytes = 0;
                for (;;)
                {
                    ReadSig();

                    int size = input.Read(buffer, 0, buffer.Length);
                    if (size < buffer.Length)
                    {
                        Array.Clear(buffer, size, buffer.Length - size);
                    }

                    var hash = sha1.ComputeHash(buffer);

                    if (sigBlockHash != null && hash.SequenceEqual(sigBlockHash) && size == sigBlockLength)
                    {
                        skipBytes += size;
                    }
                    else
                    {
                        WriteBlock(skipBytes, size, buffer);
                        skipBytes = 0;
                    }

                    if (skipBytes >= Int32.MaxValue - MainClass.BLOCK_SIZE)
                    {
                        WriteBlock(skipBytes, 0, null);
                        skipBytes = 0;
                    }

                    if (size < buffer.Length)
                    {
                        WriteBlock(skipBytes, 0, null);
                        break;
                    }
                }
            }

        }
    }
}

