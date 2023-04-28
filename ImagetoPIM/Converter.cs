using ColourQuantization;
using nQuant;
using System.Drawing;
using System.Drawing.Imaging;


namespace ImagetoPIM
{
	public class Converter
	{
		const int HEADER_LENGTH = 0x10;

		public string image_file;
		public int bit_depth;

		public bool GetFileNameFromConsole()
		{
			// get file to convert
			Console.Write("Input the name of an image file (including extension): ");
			image_file = Console.ReadLine();

			// check if file exists
			if (!File.Exists(image_file))
			{
				Console.WriteLine("Invalid Filename\nExiting Execution");
				return false;
			}
			return true;
		}

		public bool GetBitDepthFromConsole()
		{
			string input;
			Console.Write("Enter the number of bits per pixel for the pim file (4, 8, or 32):");
			input = Console.ReadLine();

			// make sure input is valid
			try
			{
				bit_depth = Convert.ToInt32(input);
			}
			catch (FormatException)
			{
				return false;
			}

			// can only be 4, 8, or 32
			if (bit_depth == 4 || bit_depth == 8 || bit_depth == 32)
				return true;
			else
				return false;
		}

		public bool WritePIMFile()
		{
			// based on the bit depth, write the pim file
			// note: pim files use RGBA order, rather than ARGB
			// note: pim alpha values end at 0x80, not 0xFF, so they are all normalized before writing
			switch (bit_depth)
			{
				case 4:
					{
						// length of palette in bytes
						const int PALETTE_LENGTH = 0x40;

						// make and quantize image file
						Bitmap image = new Bitmap(image_file);
						image = Octree.Quantize(image, 16);
						image = MedianCut.Quantize(image, 16);

                        // get byte array
                        // 4 bit pim files aren't really designed for odd value pixel counts
                        // so that has to be accounted for by adding 1 more byte if the count is odd
                        // each pixel only takes 4 bits, thus half a byte each
                        // the fraction gets contencated, which is why the extra byte has to be added in odd cases
                        // else, the file would be too short to hold every pixel
                        byte[] pim_data = new byte[HEADER_LENGTH + PALETTE_LENGTH + ((image.Width * image.Height) / 2) + ((image.Width * image.Height) % 2)];

						// get every color in the source image
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
                        palette = palette.OrderBy(a => a.R).ThenBy(a => a.G).ThenBy(a => a.B).ThenBy(a => a.A).ToList();

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

						byte red;
						byte green;
						byte blue;
						byte alpha;
						// write color palette data
						for (int i = 0; i < palette.Count; i++)
						{

							red = palette[i].R;
							green = palette[i].G;
							blue = palette[i].B;
							// normalize alpha
							alpha = (byte)((palette[i].A + 1) / 2);

							pim_data[(i * 4) + HEADER_LENGTH] = red;
							pim_data[(i * 4 + 1) + HEADER_LENGTH] = green;
							pim_data[(i * 4 + 2) + HEADER_LENGTH] = blue;
							pim_data[(i * 4 + 3) + HEADER_LENGTH] = alpha;
						}

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

						// get output file name
						string new_filename = image_file.Remove(image_file.Length - 4) + ".PIM";

						// create new pim file
						File.WriteAllBytes(new_filename, pim_data);
						if (File.Exists(new_filename))
						{
							return true;
						}
						else
						{
							return false;
						}
					}
				case 8:
					{
						// length of palette in bytes
						const int PALETTE_LENGTH = 0x400;

						// make and quantize image
						Bitmap image = new Bitmap(image_file);
						image = Octree.Quantize(image, 256);
						image = MedianCut.Quantize(image, 256);

                        // make byte array, with one byte per pixel
                        byte[] pim_data = new byte[HEADER_LENGTH + PALETTE_LENGTH + image.Width * image.Height];

                        // get every color in the source image
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
                        palette = palette.OrderBy(a => a.R).ThenBy(a => a.G).ThenBy(a => a.B).ThenBy(a => a.A).ToList();

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

						// write color palette data
						byte red;
						byte green;
						byte blue;
						byte alpha;
						for (int i = 0; i < palette.Count; i++)
						{
							red = palette[i].R;
							green = palette[i].G;
							blue = palette[i].B;
							// normalize alpha
							alpha = (byte)((palette[i].A + 1) / 2);

							pim_data[(i * 4) + HEADER_LENGTH] = red;
							pim_data[(i * 4 + 1) + HEADER_LENGTH] = green;
							pim_data[(i * 4 + 2) + HEADER_LENGTH] = blue;
							pim_data[(i * 4 + 3) + HEADER_LENGTH] = alpha;
						}

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

						// get output file name
						string new_filename = image_file.Remove(image_file.Length - 4) + ".PIM";

						// create new pim file
						File.WriteAllBytes(new_filename, pim_data);
						if (File.Exists(new_filename))
						{
							return true;
						}
						else
						{
							return false;
						}
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

						// get output file name
						string new_filename = image_file.Remove(image_file.Length - 4) + ".PIM";

						// create new pim file
						File.WriteAllBytes(new_filename, pim_data);
						if (File.Exists(new_filename))
						{
							return true;
						}
						else
						{
							return false;
						}
					}
				default:
					return false;
			}
		}

		// unused override for future uses
		public bool WritePIMFile(string filename, int depth)
		{
			// based on the bit depth, write the pim file
			// note: pim files use RGBA order, rather than ARGB
			// note: pim alpha values end at 0x80, not 0xFF, so they are all normalized before writing
			switch (depth)
			{
				case 4:
					{
						// length of palette in bytes
						const int PALETTE_LENGTH = 0x40;

						// make and quantize image file
						Bitmap image = new Bitmap(filename);
						image = Octree.Quantize(image, 16);

						// get byte array
						// 4 bit pim files aren't really designed for odd value pixel counts
						// so that has to be accounted for by adding 1 more byte if the count is odd
						// each pixel only takes 4 bits, thus half a byte each
						// the fraction gets contencated, which is why the extra byte has to be added in odd cases
						// else, the file would be too short to hold every pixel
						byte[] pim_data = new byte[HEADER_LENGTH + PALETTE_LENGTH + ((image.Width * image.Height) / 2) + ((image.Width * image.Height) % 2)];

						// get every color in the source image
						// this process is slow and should be replaced first
						bool exists;
						Color[] palette = Array.Empty<Color>();
						for (int h = 0; h < image.Height; h++)
						{
							for (int w = 0; w < image.Width; w++)
							{
								exists = false;
								Color pixel_color = image.GetPixel(w, h);

								// if the color was already found, move on to the next pixel
								for (int k = 0; k < palette.Length; k++)
								{
									if (pixel_color == palette[k])
									{
										exists = true;
										break;
									}
								}

								if (exists)
								{
									continue;
								}
								else // add the color to the array
								{
									// not all images have 16 colors in them, which is why the array has no set size
									Array.Resize(ref palette, palette.Length + 1);
									palette[palette.Length - 1] = pixel_color;
									// this is here just in case the quantization doesnt work 100% properly
									if (palette.Length >= 16)
										break;
								}
							}
							if (palette.Length >= 16)
								break;
						}

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

						byte red;
						byte green;
						byte blue;
						byte alpha;
						// write color palette data
						for (int i = 0; i < palette.Length; i++)
						{

							red = palette[i].R;
							green = palette[i].G;
							blue = palette[i].B;
							// normalize alpha
							alpha = (byte)((palette[i].A + 1) / 2);

							pim_data[(i * 4) + HEADER_LENGTH] = red;
							pim_data[(i * 4 + 1) + HEADER_LENGTH] = green;
							pim_data[(i * 4 + 2) + HEADER_LENGTH] = blue;
							pim_data[(i * 4 + 3) + HEADER_LENGTH] = alpha;
						}

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
								for (int i = 0; i < palette.Length; i++)
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

						// get output file name
						string new_filename = filename.Remove(filename.Length - 4) + ".PIM";

						// create new pim file
						File.WriteAllBytes(new_filename, pim_data);
						if (File.Exists(new_filename))
						{
							return true;
						}
						else
						{
							return false;
						}
					}
				case 8:
					{
						// length of palette in bytes
						const int PALETTE_LENGTH = 0x400;

						// make and quantize image
						Bitmap image = new Bitmap(filename);
						image = Octree.Quantize(image, 256);

						// make byte array, with one byte per pixel
						byte[] pim_data = new byte[HEADER_LENGTH + PALETTE_LENGTH + image.Width * image.Height];

						// get every color in the source image
						// very slow, especially for this encoding format
						bool exists;
						Color[] palette = Array.Empty<Color>();
						for (int h = 0; h < image.Height; h++)
						{
							for (int w = 0; w < image.Width; w++)
							{
								exists = false;
								Color pixel_color = image.GetPixel(w, h);

								// if color is already found, move on to the next pixel
								// this is why its slow
								for (int k = 0; k < palette.Length; k++)
								{
									if (pixel_color == palette[k])
									{
										exists = true;
										break;
									}
								}

								if (exists)
								{
									continue;
								}
								else
								{
									Array.Resize(ref palette, palette.Length + 1);
									palette[palette.Length - 1] = pixel_color;
									// this is here just in case the quantization doesnt work 100% properly
									if (palette.Length >= 256)
										break;
								}
							}
							if (palette.Length >= 256)
								break;
						}

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

						// write color palette data
						byte red;
						byte green;
						byte blue;
						byte alpha;
						for (int i = 0; i < palette.Length; i++)
						{
							red = palette[i].R;
							green = palette[i].G;
							blue = palette[i].B;
							// normalize alpha
							alpha = (byte)((palette[i].A + 1) / 2);

							pim_data[(i * 4) + HEADER_LENGTH] = red;
							pim_data[(i * 4 + 1) + HEADER_LENGTH] = green;
							pim_data[(i * 4 + 2) + HEADER_LENGTH] = blue;
							pim_data[(i * 4 + 3) + HEADER_LENGTH] = alpha;
						}

						// find the color for each pixel and write the index
						int index;
						for (int h = 0; h < image.Height; h++)
						{
							for (int w = 0; w < image.Width; w++)
							{
								Color pixel_color = image.GetPixel(w, h);
								index = 0;

								// find the color of the pixel then write it in
								for (int i = 0; i < palette.Length; i++)
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

						// get output file name
						string new_filename = filename.Remove(filename.Length - 4) + ".PIM";

						// create new pim file
						File.WriteAllBytes(new_filename, pim_data);
						if (File.Exists(new_filename))
						{
							return true;
						}
						else
						{
							return false;
						}
					}
				case 32:
					{
						// does not need to be quantized due to a lack of a palette
						Bitmap image = new Bitmap(filename);

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

						// get output file name
						string new_filename = filename.Remove(filename.Length - 4) + ".PIM";

						// create new pim file
						File.WriteAllBytes(new_filename, pim_data);
						if (File.Exists(new_filename))
						{
							return true;
						}
						else
						{
							return false;
						}
					}
				default:
					return false;
			}
		}
	}
}
