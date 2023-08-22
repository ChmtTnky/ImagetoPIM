using System.Drawing;
using System.Runtime.CompilerServices;
using ImageMagick;
using ImageMagick.Formats;

namespace ImagetoPIM
{
    public static class PIMConverter
    {
        const int HEADER_LENGTH = 0x10;

        // validate input before generating the output
        public static bool GeneratePIM(string input_path, int bit_depth)
        {
            if (input_path == null || input_path == string.Empty)
            {
                input_path = GetFileNameFromConsole();
                if (input_path == string.Empty)
                    return false;
            }

            if (bit_depth != 4 && bit_depth != 8 && bit_depth != 32)
            {
                bit_depth = GetBitDepthFromConsole();
                if (bit_depth == 0)
                    return false;
            }

            if (!ConvertToPIM(input_path, bit_depth))
            {
                Console.WriteLine("Could not write PIM file\n");
                return false;
            }
            return true;
        }

        // get valid file path
        private static string GetFileNameFromConsole()
        {
            // get file to convert
            Console.Write("Input the path of an image file: ");
            string image_file = Console.ReadLine();
            image_file = image_file.Trim('\"');

            // check if file exists
            if (!File.Exists(image_file))
            {
                Console.WriteLine("Invalid Filename\n");
                return string.Empty;
            }
            return image_file;
        }

        // get valid bit depth
        private static int GetBitDepthFromConsole()
        {
            string input;
            int bit_depth = 0;
            Console.Write("Enter the bit depth of the PIM file (4, 8, or 32; input 8 when you are unsure): ");
            input = Console.ReadLine();

            // make sure input is valid
            try
            {
                bit_depth = Convert.ToInt32(input);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid bit depth\n");
                return 0;
            }

            // can only be 4, 8, or 32
            if (bit_depth == 4 || bit_depth == 8 || bit_depth == 32)
                return bit_depth;
            Console.WriteLine("Invalid bit depth\n");
            return 0;
        }

        // generate a pim file from the image path with the specified bit depth
        // note: pim files use RGBA order, rather than ARGB
        // note: pim alpha values end at 0x80, not 0xFF, so they are all normalized before writing
        public static bool ConvertToPIM(string image_file, int bit_depth)
        {
            switch (bit_depth)
            {
                case 4:
                    {
                        // length of palette in bytes
                        const int PALETTE_LENGTH = 0x40;

                        // make and quantize image file to get the pallete
                        Bitmap image = null;
                        string q_image_path = QuantizeImage(image_file, 16);
                        if (q_image_path != string.Empty)
                            image = new Bitmap(q_image_path);
                        else
                        {
                            Console.WriteLine("Could not quantize image\n");
                            return false;
                        }
                        var palette = GetPalette(image);

                        // get byte array
                        // 4 bit pim files aren't really designed for odd value pixel counts
                        // so that has to be accounted for by adding 1 more byte if the count is odd
                        // each pixel only takes 4 bits, thus half a byte each
                        // the fraction gets contencated, which is why the extra byte has to be added in odd cases
                        // else, the file would be too short to hold every pixel
                        byte[] pim_data = new byte[HEADER_LENGTH + PALETTE_LENGTH + ((image.Width * image.Height) / 2) + ((image.Width * image.Height) % 2)];

                        // write width and height into header
                        pim_data[0] = ((byte)(image.Width % 256));
                        pim_data[1] = ((byte)(image.Width >> 8));
                        pim_data[2] = ((byte)(image.Height % 256));
                        pim_data[3] = ((byte)(image.Height >> 8));

                        // write rest of header values
                        pim_data[4] = 0x04;
                        pim_data[6] = 0x10;
                        pim_data[8] = 0x10;
                        pim_data[12] = 0x50;

                        WritePaletteData(pim_data, palette, HEADER_LENGTH);

                        // find the color index of each pixel and write it in
                        int index;
                        int byte_count = 0;
                        for (int h = 0; h < image.Height; h++)
                        {
                            for (int w = 0; w < image.Width; w++)
                            {
                                Color pixel_color = image.GetPixel(w, h);
                                index = 0;

                                // iterate through the palette and find the color
                                for (int i = 0; i < palette.Count; i++)
                                {
                                    if (pixel_color == palette[i])
                                    {
                                        index = i;
                                        break;
                                    }
                                }

                                // if the pixel index is even, put in lower bits
                                // else put in higher bits
                                // this is cause 4 bit pim files suck and have a wonky order
                                if (((h * image.Width) + w) % 2 == 0)
                                {
                                    pim_data[PALETTE_LENGTH + HEADER_LENGTH + byte_count] += (byte)index;
                                }
                                else
                                {
                                    pim_data[PALETTE_LENGTH + HEADER_LENGTH + byte_count] += (byte)(index * 16);
                                    byte_count++;
                                }
                            }
                        }
                        image.Dispose();
                        File.Delete(q_image_path);
                        return WritePIMFile(image_file, pim_data);
                    }
                case 8:
                    {
                        // length of palette in bytes
                        const int PALETTE_LENGTH = 0x400;

                        // make and quantize image file to get the pallete
                        Bitmap image = null;
                        string q_image_path = QuantizeImage(image_file, 256);
                        if (q_image_path != string.Empty)
                            image = new Bitmap(q_image_path);
                        else
                        {
                            Console.WriteLine("Could not quantize image\n");
                            return false;
                        }
                        var palette = GetPalette(image);

                        // make byte array, with one byte per pixel
                        byte[] pim_data = new byte[HEADER_LENGTH + PALETTE_LENGTH + image.Width * image.Height];

                        // write width and height into header
                        pim_data[0] = ((byte)(image.Width % 256));
                        pim_data[1] = ((byte)(image.Width >> 8));
                        pim_data[2] = ((byte)(image.Height % 256));
                        pim_data[3] = ((byte)(image.Height >> 8));

                        // write rest of header values
                        pim_data[4] = 0x08;
                        pim_data[7] = 0x01;
                        pim_data[8] = 0x10;
                        pim_data[12] = 0x10;
                        pim_data[13] = 0x04;

                        WritePaletteData(pim_data, palette, HEADER_LENGTH);

                        // find the color for each pixel and write the index
                        int index;
                        for (int h = 0; h < image.Height; h++)
                        {
                            for (int w = 0; w < image.Width; w++)
                            {
                                Color pixel_color = image.GetPixel(w, h);
                                index = 0;

                                // find the color of the pixel then write it in
                                for (int i = 0; i < palette.Count; i++)
                                {
                                    if (pixel_color == palette[i])
                                    {
                                        index = i;
                                        break;
                                    }
                                }

                                pim_data[PALETTE_LENGTH + HEADER_LENGTH + (h * image.Width) + w] = (byte)index;
                            }
                        }
                        image.Dispose();
                        File.Delete(q_image_path);
                        return WritePIMFile(image_file, pim_data);
                    }
                case 32:
                    {
                        // does not need to be quantized due to a lack of a palette
                        Bitmap image = new Bitmap(image_file);

                        // this format has 4 bytes per pixel
                        byte[] pim_data = new byte[HEADER_LENGTH + (4 * image.Width * image.Height)];

                        // write width and height into header
                        pim_data[0] = ((byte)(image.Width % 256));
                        pim_data[1] = ((byte)(image.Width >> 8));
                        pim_data[2] = ((byte)(image.Height % 256));
                        pim_data[3] = ((byte)(image.Height >> 8));

                        // write rest of header values
                        pim_data[4] = 0x20;
                        pim_data[12] = 0x10;

                        // for each pixel write the color values in directly
                        for (int h = 0; h < image.Height; h++)
                        {
                            for (int w = 0; w < image.Width; w++)
                            {
                                Color pixel_color = image.GetPixel(w, h);

                                pim_data[HEADER_LENGTH + (4 * ((h * image.Width) + w))] = pixel_color.R;
                                pim_data[HEADER_LENGTH + (4 * ((h * image.Width) + w)) + 1] = pixel_color.G;
                                pim_data[HEADER_LENGTH + (4 * ((h * image.Width) + w)) + 2] = pixel_color.B;
                                // normalize alpha
                                pim_data[HEADER_LENGTH + (4 * ((h * image.Width) + w)) + 3] = (byte)((pixel_color.A + 1) / 2);
                            }
                        }
                        image.Dispose();
                        return WritePIMFile(image_file, pim_data);
                    }
                default:
                    return false;
            }
        }

        // use imagemagick to quantize the colors for the given bit depth's color limit (16 for 4 bit; 256 for 8 bit; 32 bit does not quantize)
        // note: images with extremly low color variation can significantly less colors than intended at lower bit depths
        // ex: an image that is mostly white with a transparent background can be made 100% transparent at a bit depth of 4
        private static string QuantizeImage(string input_path, int count)
        {
            ImageMagick.QuantizeSettings settings = new ImageMagick.QuantizeSettings();
            settings.ColorSpace = ColorSpace.Lab;
            settings.DitherMethod = DitherMethod.No;
            settings.Colors = count;

            string output_path = "q_" + Path.GetFileNameWithoutExtension(input_path) + ".png";
            var m_image = new ImageMagick.MagickImage(input_path);
            if (m_image.TotalColors > count)
                m_image.Quantize(settings);
            m_image.Write(output_path, MagickFormat.Png);
            if (File.Exists(output_path))
                return output_path;
            return string.Empty;
        }

        private static List<Color> GetPalette(Bitmap image)
        {
            var palette = new List<Color>();
            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    if (palette.Contains(image.GetPixel(w, h)))
                        continue;
                    palette.Add(image.GetPixel(w, h));
                }
            }
            return palette.OrderBy(a => a.R).ThenBy(a => a.G).ThenBy(a => a.B).ThenBy(a => a.A).ToList();
        }

        private static void WritePaletteData(byte[] pim_data, List<Color> palette, int offset)
        {
            for (int i = 0; i < palette.Count; i++)
            {
                pim_data[(i * 4) + HEADER_LENGTH] = palette[i].R;
                pim_data[(i * 4 + 1) + HEADER_LENGTH] = palette[i].G;
                pim_data[(i * 4 + 2) + HEADER_LENGTH] = palette[i].B;
                pim_data[(i * 4 + 3) + HEADER_LENGTH] = (byte)((palette[i].A + 1) / 2);
            }
        }

        private static bool WritePIMFile(string input_path, byte[] pim_data)
        {
            string output_path = Path.GetFileNameWithoutExtension(input_path) + ".PIM";
            File.WriteAllBytes(output_path, pim_data);
            if (File.Exists(output_path))
                return true;
            return false;
        }
    }
}
