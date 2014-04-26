using System;
using System.IO;
using System.Security.Cryptography;

namespace BulkSync
{
    public class Signature : IDisposable
    {
        Stream input;
        Stream signature;

        public Signature(string inputPath, string signaturePath)
        {
            input = string.IsNullOrEmpty(inputPath) ? Console.OpenStandardInput() : File.OpenRead(inputPath);
            signature = string.IsNullOrEmpty(signaturePath) ? Console.OpenStandardOutput() : File.Create(signaturePath);
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
        }

        #endregion

        void Write(byte[] data)
        {
            signature.Write(data, 0, data.Length);
        }

        public void Run()
        {
            Write(MainClass.SIG_HEADER);

            var buffer = new byte[MainClass.BLOCK_SIZE];
            using (var sha1 = new SHA1Managed())
            {
                for (;;)
                {
                    int size = input.Read(buffer, 0, buffer.Length);
                    if (size < buffer.Length)
                    {
                        Array.Clear(buffer, size, buffer.Length - size);
                    }

                    var hash = sha1.ComputeHash(buffer);
                    Write(hash);

                    if (size < buffer.Length)
                    {
                        Write(BitConverter.GetBytes(size));
                        break;
                    }
                }
            }
        }
    }
}

