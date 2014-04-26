using System;

namespace BulkSync
{
    public class CommandLineOptions
    {
        public enum Mode {
            Bad,
            Signature,
            Delta,
            ReverseDelta,
            Patch,
        }

        public Mode RunMode;
        public string Input, Target, Signature, Delta, ReverseDelta;

        public CommandLineOptions(string[] args)
        {
            var en = args.GetEnumerator();

            if (!en.MoveNext())
            {
                RunMode = Mode.Bad;
                return;
            }

            switch (Convert.ToString(en.Current).ToLowerInvariant())
            {
                case "s":
                case "sig":
                case "signature":
                    RunMode = Mode.Signature;

                    if (en.MoveNext())
                    {
                        Input = (string)en.Current;
                        if (en.MoveNext())
                        {
                            Signature = (string)en.Current;
                        }
                    }

                    break;

                case "d":
                case "delta":
                    RunMode = Mode.Delta;

                    if (!en.MoveNext())
                    {
                        throw new ArgumentNullException();
                    }

                    Input = (string)en.Current;
                    if (en.MoveNext())
                    {
                        Signature = (string)en.Current;
                        if (en.MoveNext())
                        {
                            Delta = (string)en.Current;
                        }
                    }

                    break;

                case "p":
                case "pat":
                case "patch":
                    RunMode = Mode.Patch;

                    if (!en.MoveNext())
                    {
                        throw new ArgumentNullException();
                    }

                    Target = (string)en.Current;
                    if (en.MoveNext())
                    {
                        Delta = (string)en.Current;
                    }

                    break;

                case "r":
                case "rd":
                case "rev":
                case "reverse":
                case "reversedelta":
                    RunMode = Mode.ReverseDelta;

                    if (!en.MoveNext())
                    {
                        throw new ArgumentNullException();
                    }

                    Target = (string)en.Current;
                    if (en.MoveNext())
                    {
                        Delta = (string)en.Current;
                        if (en.MoveNext())
                        {
                            ReverseDelta = (string)en.Current;
                        }
                    }

                    break;

                default:
                    RunMode = Mode.Bad;
                    return;
            }

        }

        public void PrintUsage()
        {
            Console.Error.WriteLine("Usage:");
        }

    }
}

