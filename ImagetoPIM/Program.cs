using ColourQuantization;
using System.Drawing;

const int HEADER_LENGTH = 0x10;

// get file to convert
Console.Write("Input the name of an image file (including extension): ");
string imgfile = Console.ReadLine();

// check if file exists
if (!File.Exists(imgfile))
{
    Console.WriteLine("Invalid Filename\nExiting Execution");
    return;
}

// get bit depth
string input;
bool valid_input = false;
int bit_depth = 0;
while (valid_input == false)
{
    valid_input = true;
    Console.Write("Enter the number of bits per pixel for the pim file (4, 8, or 32):");
    input = Console.ReadLine();
    try
    {
        bit_depth = Convert.ToInt32(input);
    }
    catch (FormatException)
    {
        valid_input = false;
    }
    if (bit_depth != 4 && bit_depth != 8 & bit_depth != 32)
        valid_input = false;
}

switch (bit_depth)
{
    case 4:
        {
            const int PALETTE_LENGTH = 0x40;

            Bitmap image = new Bitmap(imgfile);
            image = Octree.Quantize(image, 16);

            byte[] pimdata = new byte[HEADER_LENGTH + PALETTE_LENGTH + ((image.Width * image.Height) / 2) + ((image.Width * image.Height) % 2)];

            // get every color in the source image
            Color[] palette = Array.Empty<Color>();
            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    bool exists = false;
                    Color pixelcolor = image.GetPixel(w, h);

                    for (int k = 0; k < palette.Length; k++)
                    {
                        if (pixelcolor == palette[k])
                            exists = true;
                    }

                    if (exists)
                    {
                        continue;
                    }
                    else
                    {
                        Array.Resize(ref palette, palette.Length + 1);
                        palette[palette.Length - 1] = pixelcolor;
                        if (palette.Length >= 16)
                            break;
                    }
                }
                if (palette.Length >= 16)
                    break;
            }

            // write width and height into header
            pimdata[0] = ((byte)(image.Width % 256));
            pimdata[1] = ((byte)(image.Width >> 8));
            pimdata[2] = ((byte)(image.Height % 256));
            pimdata[3] = ((byte)(image.Height >> 8));

            // write rest of header values
            pimdata[4] = 0x04;
            pimdata[6] = 0x10;
            pimdata[8] = 0x10;
            pimdata[12] = 0x50;

            // write color palette data
            for (int i = 0; i < palette.Length; i++)
            {
                byte red = palette[i].R;
                byte green = palette[i].G;
                byte blue = palette[i].B;
                byte alpha = (byte)((palette[i].A + 1) / 2);

                pimdata[(i * 4) + HEADER_LENGTH] = red;
                pimdata[(i * 4 + 1) + HEADER_LENGTH] = green;
                pimdata[(i * 4 + 2) + HEADER_LENGTH] = blue;
                pimdata[(i * 4 + 3) + HEADER_LENGTH] = alpha;
            }

            // find the color index of each pixel and write it in
            int byte_count = 0;
            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    Color pixelcolor = image.GetPixel(w, h);
                    int index = 0;

                    for (int i = 0; i < palette.Length; i++)
                    {
                        if (pixelcolor == palette[i])
                            index = i;
                    }

                    // if the pixel is even, put in lower bits
                    // else put in higher bits
                    if (((h * image.Width) + w) % 2 == 0)
                    {
                        pimdata[PALETTE_LENGTH + HEADER_LENGTH + byte_count] += (byte)index;
                    }
                    else
                    {
                        pimdata[PALETTE_LENGTH + HEADER_LENGTH + byte_count] += (byte)(index * 16);
                        byte_count++;
                    }
                }
            }

            // get output file name
            string newfilename = imgfile.Remove(imgfile.Length - 4) + ".PIM";

            // create new pim file
            File.WriteAllBytes(newfilename, pimdata);
            if (File.Exists(newfilename))
                Console.WriteLine("New PIM File saved as " + newfilename);
            else
                Console.WriteLine("Could not create file\nExiting Execution");

            break;
        }
    case 8:
        {
            const int PALETTE_LENGTH = 0x400;

            Bitmap image = new Bitmap(imgfile);
            image = Octree.Quantize(image, 256);

            byte[] pimdata = new byte[HEADER_LENGTH + PALETTE_LENGTH + image.Width * image.Height];

            // get every color in the source image
            Color[] palette = Array.Empty<Color>();
            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    bool exists = false;
                    Color pixelcolor = image.GetPixel(w, h);

                    for (int k = 0; k < palette.Length; k++)
                    {
                        if (pixelcolor == palette[k])
                            exists = true;
                    }

                    if (exists)
                    {
                        continue;
                    }
                    else
                    {
                        Array.Resize(ref palette, palette.Length + 1);
                        palette[palette.Length - 1] = pixelcolor;
                        if (palette.Length >= 256)
                            break;
                    }
                }
                if (palette.Length >= 256)
                    break;
            }

            // write width and height into header
            pimdata[0] = ((byte)(image.Width % 256));
            pimdata[1] = ((byte)(image.Width >> 8));
            pimdata[2] = ((byte)(image.Height % 256));
            pimdata[3] = ((byte)(image.Height >> 8));

            // write rest of header values
            pimdata[4] = 0x08;
            pimdata[7] = 0x01;
            pimdata[8] = 0x10;
            pimdata[12] = 0x10;
            pimdata[13] = 0x04;

            // write color palette data
            for (int i = 0; i < palette.Length; i++)
            {
                byte red = palette[i].R;
                byte green = palette[i].G;
                byte blue = palette[i].B;
                byte alpha = (byte)((palette[i].A + 1) / 2);

                pimdata[(i * 4) + HEADER_LENGTH] = red;
                pimdata[(i * 4 + 1) + HEADER_LENGTH] = green;
                pimdata[(i * 4 + 2) + HEADER_LENGTH] = blue;
                pimdata[(i * 4 + 3) + HEADER_LENGTH] = alpha;
            }

            // find the color for each pixel and write the index
            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    Color pixelcolor = image.GetPixel(w, h);
                    int index = 0;

                    for (int i = 0; i < palette.Length; i++)
                    {
                        if (pixelcolor == palette[i])
                            index = i;
                    }

                    pimdata[PALETTE_LENGTH + HEADER_LENGTH + (h * image.Width) + w] = (byte)index;
                }
            }

            // get output file name
            string newfilename = imgfile.Remove(imgfile.Length - 4) + ".PIM";

            // create new pim file
            File.WriteAllBytes(newfilename, pimdata);
            if (File.Exists(newfilename))
                Console.WriteLine("New PIM File saved as " + newfilename);
            else
                Console.WriteLine("Could not create file\nExiting Execution");

            break;
        }
    case 32:
        {
            // does not need to be quantized due to a lack of a palette
            Bitmap image = new Bitmap(imgfile);

            byte[] pimdata = new byte[HEADER_LENGTH + (4 * image.Width * image.Height)];

            // write width and height into header
            pimdata[0] = ((byte)(image.Width % 256));
            pimdata[1] = ((byte)(image.Width >> 8));
            pimdata[2] = ((byte)(image.Height % 256));
            pimdata[3] = ((byte)(image.Height >> 8));

            // write rest of header values
            pimdata[4] = 0x20;
            pimdata[12] = 0x10;

            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    Color pixel_color = image.GetPixel(w, h);

                    pimdata[HEADER_LENGTH + (4 * ((h * image.Width) + w))] = pixel_color.R;
                    pimdata[HEADER_LENGTH + (4 * ((h * image.Width) + w)) + 1] = pixel_color.G;
                    pimdata[HEADER_LENGTH + (4 * ((h * image.Width) + w)) + 2] = pixel_color.B;
                    pimdata[HEADER_LENGTH + (4 * ((h * image.Width) + w)) + 3] = (byte)((pixel_color.A + 1) / 2);
                }
            }

            // get output file name
            string newfilename = imgfile.Remove(imgfile.Length - 4) + ".PIM";

            // create new pim file
            File.WriteAllBytes(newfilename, pimdata);
            if (File.Exists(newfilename))
                Console.WriteLine("New PIM File saved as " + newfilename);
            else
                Console.WriteLine("Could not create file\nExiting Execution");

            break;
        }
}