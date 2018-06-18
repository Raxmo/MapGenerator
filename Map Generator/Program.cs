using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace MapGenerator
{
	class Program
	{
		public static Random rng = new Random();

		unsafe public static void Seed(int px, int py)
		{
			using (var seed = new Bitmap(256, 256, PixelFormat.Format24bppRgb))
			{
				// lock the array for direct access
				var bitmapData = seed.LockBits(new Rectangle(0, 0, seed.Width, seed.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
				// get the pointer
				var scan0Ptr = (int*)bitmapData.Scan0;
				// get the stride
				var stride = bitmapData.Stride / 4;

				for (var x = 0; x < seed.Width; x++)
					for (var y = 0; y < seed.Height; y++)
					{
						var val = rng.Next(0, 255);
						*(scan0Ptr + x + y * stride) = Color.FromArgb(255, val, val, val).ToArgb();
					}

				seed.UnlockBits(bitmapData);

				seed.Save("chunk," + px + "," + py + ".jpeg", ImageFormat.Jpeg);
			}
		}

		public static void Perlin(int px, int py)
		{
			Bitmap seed = new Bitmap("chunk," + px + "," + py + ".jpeg");

			Bitmap output = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


			for (int y = 0; y < 256; y++)
			{
				for (int x = 0; x < 256; x++)
				{
					double noise = 0.0;
					double scale = 1.0;
					double acc = 0.0;

					for (int o = 0; o < 8; o++)
					{
						int pitch = 256 >> o;
						int sampleX1 = (x / pitch) * pitch;
						int sampleY1 = (y / pitch) * pitch;


						int sampleX2 = (sampleX1 + pitch);
						int sampleY2 = (sampleY1 + pitch);

						sampleX2 = (sampleX2 == 256) ? 255 : sampleX2;
						sampleY2 = (sampleY2 == 256) ? 255 : sampleY2;

						double Xblend = (double)(x - sampleX1) / (double)pitch;
						double Yblend = (double)(y - sampleY1) / (double)pitch;

						// interpolate between the two points
						double Tsample = ((1 - Xblend) * ((double)seed.GetPixel(sampleX1, sampleY1).R / 255.0)) + (Xblend * ((double)seed.GetPixel(sampleX2, sampleY1).R / 255.0));
						double Bsample = ((1 - Xblend) * ((double)seed.GetPixel(sampleX1, sampleY2).R / 255.0)) + (Xblend * ((double)seed.GetPixel(sampleX2, sampleY2).R / 255.0));

						noise += (((1 - Yblend) * Tsample) + (Yblend * Bsample)) * scale;
						acc += scale;
						scale = scale * 0.6;
					}

					noise = noise / acc;

					noise = noise * 255.0;

					output.SetPixel(x, y, Color.FromArgb(255, (int)(noise), (int)(noise), (int)(noise)));
				}
			}
			seed.Dispose();
			output.Save("chunk," + px + "," + py + ".jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
			output.Dispose();
		}

		/* Steps for map generation
		 * - define biomes
		 * - set heightmap for each biome
		 * - generate map
		 * - colorize
		 * - clean foulder
		 */

		enum biome : byte { plains, mountain, swamp, river, forest, ruins, desert, badlands };
		static Color[] biomeColor = new Color[] {Color.FromArgb(0 << 5, Color.GreenYellow),
												 Color.FromArgb(1 << 5, Color.Gray),
												 Color.FromArgb(2 << 5, Color.MediumSeaGreen),
												 Color.FromArgb(3 << 5, Color.SkyBlue),
												 Color.FromArgb(4 << 5, Color.ForestGreen),
												 Color.FromArgb(5 << 5, Color.AntiqueWhite),
												 Color.FromArgb(6 << 5, Color.Beige),
												 Color.FromArgb(7 << 5, Color.Firebrick)};

		public static void Generate(int depth, int points)
		{
			/* Generation steps
			 * 
			 * [COMPLETED]
			 * - define global biome regions
			 *   - place random seed points and store them
			 * 
			 * [TODO]
			 * - define global biome regions
			 *   - for every pixel, check to see what seed point it is closest to
			 *   - color chosen pixel to match the color of closest point
			 * - voronoi declaration of reagions
			 * - define fine biome regions
			 *   - voronoi declaration of biomes per chunk
			 * - perlin generation
			 *   - generate seed noise from voronoi declaration
			 *   - generate perlin from seed noise
			 *   - colorise
			 */

			// seed an image with points and save those points

			using (var output = new Bitmap((depth << 1) + 1, (depth << 1) + 1))
			{
				//Point[] seeds = new Point[points];

				//for (int i = 0; i < points; i++)
				//{
				//	seeds[i] = new Point(rng.Next(0, (depth << 1) + 1), rng.Next(0, (depth << 1) + 1));

				//	var Biome = biomeColor[rng.Next(8)];

				//	output.SetPixel(seeds[i].X, seeds[i].Y, Color.FromArgb(Biome.A, 0, 0, 0));
				//}


				int percent = 0;

				Console.WriteLine("Initiating Voronoi construction...");

				/* Poisson disk sampling
				 * 
				 */

				// check to see what seed in closest, and set the chosen pixel's color to the same color
				for (int x = 0; x < output.Width; x++)
				{
					for (int y = 0; y < output.Height; y++)
					{
						var curPoint = new Point();
						int curDistance = int.MaxValue;

						for (int i = 0; i < seeds.Length; i++)
						{
							if (((seeds[i].X - x) * (seeds[i].X - x) + (seeds[i].Y - y) * (seeds[i].Y - y)) <= curDistance)
							{
								curDistance = ((seeds[i].X - x) * (seeds[i].X - x) + (seeds[i].Y - y) * (seeds[i].Y - y));
								curPoint = seeds[i];
							}
						}

						output.SetPixel(x, y, output.GetPixel(curPoint.X, curPoint.Y));
					}
					percent = ((100 * x) / output.Width) + 1;

					Console.Write("\r {0}% complete.     ", percent);
				}

				percent = 0;
				Console.WriteLine("");
				Console.WriteLine("Coloring the map...");

				for (int x = 0; x < output.Width; x++)
				{
					for (int y = 0; y < output.Height; y++)
					{
						switch (output.GetPixel(x, y).A >> 5)
						{
							case 0:
								output.SetPixel(x, y, Color.FromArgb(255, biomeColor[0]));
								break;
							case 1:
								output.SetPixel(x, y, Color.FromArgb(255, biomeColor[1]));
								break;
							case 2:
								output.SetPixel(x, y, Color.FromArgb(255, biomeColor[2]));
								break;
							case 3:
								output.SetPixel(x, y, Color.FromArgb(255, biomeColor[3]));
								break;
							case 4:
								output.SetPixel(x, y, Color.FromArgb(255, biomeColor[4]));
								break;
							case 5:
								output.SetPixel(x, y, Color.FromArgb(255, biomeColor[5]));
								break;
							case 6:
								output.SetPixel(x, y, Color.FromArgb(255, biomeColor[6]));
								break;
							default:
								output.SetPixel(x, y, Color.FromArgb(255, biomeColor[7]));
								break;
						}
					}
					percent = ((100 * x) / output.Width) + 1;

					Console.Write("\r {0}% complete.     ", percent);
				}

				Console.WriteLine("");
				output.Save("test.bmp");
			}
		}

		static void Main(string[] args)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			Console.WriteLine("Starting generation, please wait, will take some time.");

			Generate(720, 10);

			sw.Stop();
			Console.WriteLine("Finished generation in: " + sw.Elapsed);
			Console.ReadKey(true);
		}
	}
}
