using ColourQuantization;
using System.Drawing;

const int PALETTE_START = 0x10;
const int PIXEL_START = 0x400;

Console.Write("Input the name of an image file (including extension): ");
string imgfile = Console.ReadLine();

// check if file exists
if (!File.Exists(imgfile))
{
    Console.WriteLine("Invalid Filename");
    return;
}

// reduce image palette size to 256
Bitmap image = new Bitmap(imgfile);
image = Octree.Quantize(image, 256);

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
        }
    }
}

// create byte array to hold file data
byte[] pimdata = Array.Empty<byte>();
Array.Resize(ref pimdata, PALETTE_START + PIXEL_START + image.Width * image.Height);

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

for (int i = 0; i < palette.Length; i++)
{
    byte red = palette[i].R;
    byte green = palette[i].G;
    byte blue = palette[i].B;
    byte alpha = (byte)((palette[i].A + 1) / 2);

    pimdata[(i * 4) + PALETTE_START] = red;
    pimdata[(i * 4 + 1) + PALETTE_START] = green;
    pimdata[(i * 4 + 2) + PALETTE_START] = blue;
    pimdata[(i * 4 + 3) + PALETTE_START] = alpha;
}

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

        pimdata[PIXEL_START + PALETTE_START + (h * image.Width) + w] = (byte)index;
    }
}

string newfilename = string.Empty;
for (int i = 0; i < imgfile.Length - 4; i++)
    newfilename += imgfile[i];
newfilename += ".PIM";

File.WriteAllBytes(newfilename, pimdata);
if (File.Exists(newfilename))
    Console.WriteLine("New PIM File saved as " + newfilename);