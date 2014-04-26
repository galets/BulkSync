using System;
using System.Text;

namespace BulkSync
{
    public class MainClass
    {
        public const int BLOCK_SIZE = 1024 * 16;
        public static readonly byte[] SIG_HEADER = Encoding.ASCII.GetBytes("BSSIG01");
        public static readonly byte[] DELTA_HEADER = Encoding.ASCII.GetBytes("BSDELTA01");
        public static readonly int HASH_SIZE = 20;

        public static int Main(params string[] args)
        {
            var cmdline = new CommandLineOptions(args);

            if (cmdline.RunMode == CommandLineOptions.Mode.Signature)
            {
                using (var sig = new Signature(cmdline.Input, cmdline.Signature))
                {
                    sig.Run();
                    return 0;
                }
            }
            else if (cmdline.RunMode == CommandLineOptions.Mode.Delta)
            {
                using (var delta = new Delta(cmdline.Input, cmdline.Signature, cmdline.Delta))
                {
                    delta.Run();
                    return 0;
                }
            }
            else if (cmdline.RunMode == CommandLineOptions.Mode.Patch)
            {
                using (var patch = new Patch(cmdline.Delta, cmdline.Target))
                {
                    patch.Run();
                    return 0;
                }
            }
            else if (cmdline.RunMode == CommandLineOptions.Mode.ReverseDelta)
            {
                using (var reverseDelta = new ReverseDelta(cmdline.Target, cmdline.Delta, cmdline.ReverseDelta))
                {
                    reverseDelta.Run();
                    return 0;
                }
            }
            else
            {
                cmdline.PrintUsage();
                return 1;
            }
        }
    }
}
