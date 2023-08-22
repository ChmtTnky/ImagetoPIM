namespace ImagetoPIM
{
    public class Program
    {
        static void Main(params string[] args)
        {
            if (args.Length == 0)
            {
                PIMConverter.GeneratePIM(null, 0);
            }
            else if (args.Length == 1)
            {
                if (File.Exists(args[0]))
                {
                    PIMConverter.GeneratePIM(args[0], 0);
                }
            }
            else if (args.Length == 2)
            {
                if (File.Exists(args[0]))
                {
                    try
                    {
                        int bit_depth = Convert.ToInt32(args[1]);
                        PIMConverter.GeneratePIM(args[0], bit_depth);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("Bit depth is not a number\n");
                        return;
                    }
                }
            }
        }
    }
}