namespace ImagetoPIM
{
    public class Program
    {
        static void Main(params string[] args)
        {
            Converter converter = new Converter();

            if (args.Length == 0)
            {
                while (!converter.GetFileNameFromConsole()) ;
                while (!converter.GetBitDepthFromConsole()) ;
                converter.WritePIMFile();
            }
            else if (args.Length == 1)
            {
                if (File.Exists(args[0]))
                {
                    converter.imgfile = args[0];
                    while (!converter.GetBitDepthFromConsole()) ;
                    converter.WritePIMFile();
                }
            }
            else if (args.Length == 2)
            {
                if (File.Exists(args[0]))
                {
                    try
                    {
                        converter.bit_depth = Convert.ToInt32(args[1]);
                    }
                    catch (FormatException)
                    {
                        return;
                    }
                    converter.imgfile = args[0];
                    converter.WritePIMFile();
                }
            }
        }
    }
}