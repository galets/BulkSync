using NUnit.Framework;
using System;
using System.Linq;
using System.IO;
using BulkSync;

namespace BulkSyncNUnit
{
    [TestFixture()]
    public class Test
    {
        public string workplaceDir;
        public Random random;

        [SetUp]
        public void SetUp()
        {
            workplaceDir = "/tmp/bsut-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(workplaceDir);

            random = new Random();
        }

        [TearDown]
        public void FixtureTearDown()
        {
            if (!string.IsNullOrEmpty(workplaceDir))
            {
                Directory.Delete(workplaceDir, true);
            }
        }

        string CreateRandomFile(string name, int size)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = Guid.NewGuid().ToString("N");
            }

            var fullName = Path.Combine(workplaceDir, name);
            var buf = new byte[4096];
            using (var f = File.Create(fullName))
            {
                while (size > 0)
                {
                    var size1 = Math.Min(size, buf.Length);
                    random.NextBytes(buf);
                    f.Write(buf, 0, size1);
                    size -= size1;
                }
            }

            return fullName;
        }

        bool FilesAreSame(string file1, string file2)
        {
            using (var br1 = new BinaryReader(File.OpenRead(file1)))
            using (var br2 = new BinaryReader(File.OpenRead(file2)))
            {
                for (;;)
                {
                    var b1 = br1.ReadBytes(4096);
                    var b2 = br2.ReadBytes(4096);
                    if (!b1.SequenceEqual(b2))
                    {
                        return false;
                    }

                    if (b1.Length == 0)
                    {
                        return true;
                    }
                }
            }
        }

        void DoFullCirle(string file1, string file2)
        {
            var file1bak = file1 + ".bak";
            var sig = file1 + ".sig";
            var delta = file1 + ".delta";
            var revdelta = file1 + ".revdelta";

            File.Copy(file1, file1bak);
            Assert.AreEqual(0, MainClass.Main("sig", file1, sig));
            Assert.AreEqual(0, MainClass.Main("delta", file2, sig, delta));
            Assert.AreEqual(0, MainClass.Main("reversedelta", file1, delta, revdelta));
            Assert.AreEqual(0, MainClass.Main("patch", file1, delta));
            Assert.IsTrue(FilesAreSame(file1, file2));
            Assert.AreEqual(0, MainClass.Main("patch", file1, revdelta));
            Assert.IsTrue(FilesAreSame(file1, file1bak));
        }

        [Test()]
        public void TestCaseSimple()
        {
            var initFile = CreateRandomFile(null, MainClass.BLOCK_SIZE);
            var otherFile = CreateRandomFile(null, MainClass.BLOCK_SIZE);
            DoFullCirle(initFile, otherFile);
        }

        [Test()]
        public void TestCaseLargerDiffSizes()
        {
            for (int i = 0; i < 10; ++i)
            {
                var initFile = CreateRandomFile(null, random.Next(20, 1024 * 1024 * 30));
                var otherFile = CreateRandomFile(null, random.Next(20, 1024 * 1024 * 30));

                DoFullCirle(initFile, otherFile);
            }
        }

        [Test()]
        public void TestCaseSomehowSimilarFiles()
        {
            for (int i = 0; i < 100; ++i)
            {
                var initFile = CreateRandomFile(null, random.Next(1024 * 1024 * 1, 1024 * 1024 * 10));
                var otherFile = initFile + ".other";

                using (var br1 = new BinaryReader(File.OpenRead(initFile)))
                using (var oth1 = File.Create(otherFile))
                {
                    for (;;)
                    {
                        int size = random.Next(MainClass.BLOCK_SIZE, MainClass.BLOCK_SIZE * 30);
                        var b1 = br1.ReadBytes(size);

                        if (random.Next(2) == 0)
                        {
                            random.NextBytes(b1);
                        }

                        if (random.Next(100) == 0)
                        {
                            break;
                        }

                        if (random.Next(100) == 0)
                        {
                            b1 = new byte[random.Next(10, MainClass.BLOCK_SIZE * 2)];
                            random.NextBytes(b1);
                        }

                        oth1.Write(b1, 0, b1.Length);
                    }
                }

                DoFullCirle(initFile, otherFile);
            }
        }
    }
}

