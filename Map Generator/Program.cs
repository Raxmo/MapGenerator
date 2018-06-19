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
												 Color.FromArgb(6 << 5, Color.Yellow),
												 Color.FromArgb(7 << 5, Color.Firebrick)};





		public static Point[,] Poisson(int depth, Bitmap source, bool isFirstItteration)
		{
			var r = depth >> 1;
			var k = 30;

			var cellSize = (r >> 1) * Math.Sqrt(2);
			var cellinverse = Math.Sqrt(2) / r;
			var size = (int)Math.Ceiling(source.Width * cellinverse);

			var grid = new Point[size, size];
			for(int i = 0; i < size; i++)
			{
				for(int j = 0; j < size; j++)
				{
					grid[i, j] = new Point(-1, -1);
				}
			}

			var sample0 = (isFirstItteration) ? new Point(source.Width >> 1, source.Height >> 1) : new Point(rng.Next(source.Width), rng.Next(source.Height));
			grid[(int)(sample0.X * cellinverse), (int)(sample0.Y * cellinverse)] = sample0;
			var active = new List<Point> { sample0 };

			if (isFirstItteration)
			{
				source.SetPixel(sample0.X, sample0.Y, biomeColor[(int)biome.badlands]);
			}

			while(active.Count > 0)
			{
				var workingIndex = rng.Next(active.Count);
				var working = active[workingIndex];

				var foundValid = false;
				for(int i = 0; i < k; i++)
				{
					var sa = 2 * rng.NextDouble() * Math.PI;
					var sr = (rng.NextDouble() * r) + r;

					var sx = sr * Math.Cos(sa) + working.X;
					var sy = sr * Math.Sin(sa) + working.Y;

					sx = Math.Max(0, sx);
					sy = Math.Max(0, sy);

					sx = Math.Min(sx, (depth << 1));
					sy = Math.Min(sy, (depth << 1));


					var sample = new Point((int)sx, (int)sy);
					var sgrid = new Point((int)Math.Floor(sample.X * cellinverse), (int)Math.Floor(sample.Y * cellinverse));

					var isValid = true;

					for (int j = -1; j <= 1; j++)
					{
						for (int h = -1; h <= 1; h++)
						{
							if ((j + sgrid.X) >= 0 && (j + sgrid.X) < size && (h + sgrid.Y) >= 0 && (h + sgrid.Y) < size)
							{
								var neighbor = grid[sgrid.X + j, sgrid.Y + h];

								if (neighbor.X != -1)
								{
									var dist = ((neighbor.X - sample.X) * (neighbor.X - sample.X)) + ((neighbor.Y - sample.Y) * (neighbor.Y - sample.Y));
									if (dist < (r * r))
									{
										isValid = false;
									}
								}
							}
						}
					}

					if(isValid)
					{
						foundValid = true;
						grid[sgrid.X, sgrid.Y] = sample;
						active.Add(sample);
					}
				}
				
				if(!foundValid)
				{
					active.RemoveAt(workingIndex);
				}
			}

			for(int i = 0; i < size; i++)
			{
				for(int j = 0; j < size; j++)
				{
					if (grid[i, j] != new Point(-1, -1))
					{
						if (isFirstItteration)
						{
							var isOut = grid[i, j] == sample0;

							if (isOut)
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, biomeColor[(int)biome.badlands]);
							}
							else if (-(grid[i, j].Y - sample0.Y) >= Math.Abs(grid[i, j].X - sample0.X))
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, biomeColor[(int)biome.desert]);
							}
							else if (grid[i, j].X - sample0.X > Math.Abs(grid[i, j].Y - sample0.Y))
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, biomeColor[(int)biome.plains]);
							}
							else if (-(grid[i, j].X - sample0.X) > Math.Abs(grid[i, j].Y - sample0.Y))
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, biomeColor[(int)biome.swamp]);
							}
							else
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, biomeColor[(int)biome.forest]);
							}
						}
						else
						{
							if(rng.NextDouble() < 0.03)
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, biomeColor[(int)biome.ruins]);
							}
							else if(rng.NextDouble() < 0.03)
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, biomeColor[(int)biome.mountain]);
							}
							else if (rng.NextDouble() < 0.03)
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, biomeColor[(int)biome.river]);
							}
							else
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, source.GetPixel(grid[i, j].X, grid[i, j].Y));
							}
						}
					}
				}
			}

			return grid;
		}

		public static void Voronoi(Point[,] seeds, Bitmap source, bool isFirstItteration, int depth)
		{

			var r = depth >> 1;

			var cellSize = (r >> 1) * Math.Sqrt(2);
			var cellinverse = Math.Sqrt(2) / r;
			var size = (int)Math.Ceiling(source.Width * cellinverse);

			for (int x = 0; x < source.Width; x++)
			{
				for (int y = 0; y < source.Height; y++)
				{
					var curPoint = new Point();

					var curgrid = new Point((int)Math.Floor(x * cellinverse), (int)Math.Floor(y * cellinverse));

					int curDistance = int.MaxValue;

					for (int i = -2; i <= 2; i++)
					{
						for (int j = -2; j <= 2; j++)
						{
							if ((i + curgrid.X) >= 0 && (i + curgrid.X) < size && (j + curgrid.Y) >= 0 && (j + curgrid.Y) < size)
							{
								var neighbor = seeds[curgrid.X + i, curgrid.Y + j];

								if (neighbor != new Point(-1, -1))
								{
									var wdist = ((neighbor.X - x) * (neighbor.X - x)) + ((neighbor.Y - y) * (neighbor.Y - y));

									if (wdist < curDistance)
									{
										curPoint = neighbor;
										curDistance = wdist;
									}
								}
							}
						}
					}
					if(isFirstItteration)
					{
						var o = new Point(source.Width >> 1, source.Height >> 1);
						var ndist = Math.Abs(((x - o.X) * (x - o.X)) + ((y - o.Y) * (y - o.Y)) - (depth * depth));
						var dist = ((x - o.X) * (x - o.X)) + ((y - o.Y) * (y - o.Y));
						if(ndist < curDistance)
						{
							curPoint = new Point(source.Width >> 1, source.Height >> 1);
						}
					}
					source.SetPixel(x, y, source.GetPixel(curPoint.X, curPoint.Y));
				}
			}
		}

		public static void Colorize(Bitmap source, bool isFirstItteration, int depth)
		{
			for (int x = 0; x < source.Width; x++)
			{
				for (int y = 0; y < source.Height; y++)
				{
					if (isFirstItteration)
					{
						var o = new Point(source.Width >> 1, source.Height >> 1);
						var ndist = Math.Abs(((x - o.X) * (x - o.X)) + ((y - o.Y) * (y - o.Y)) - (depth * depth));
						var dist = ((x - o.X) * (x - o.X)) + ((y - o.Y) * (y - o.Y));
						if (dist > (depth * depth))
						{
							source.SetPixel(x, y, biomeColor[(int)biome.badlands]);
						}
					}
					switch (source.GetPixel(x, y).A >> 5)
					{
						case 0:
							source.SetPixel(x, y, Color.FromArgb(255, biomeColor[0]));
							break;
						case 1:
							source.SetPixel(x, y, Color.FromArgb(255, biomeColor[1]));
							break;
						case 2:
							source.SetPixel(x, y, Color.FromArgb(255, biomeColor[2]));
							break;
						case 3:
							source.SetPixel(x, y, Color.FromArgb(255, biomeColor[3]));
							break;
						case 4:
							source.SetPixel(x, y, Color.FromArgb(255, biomeColor[4]));
							break;
						case 5:
							source.SetPixel(x, y, Color.FromArgb(255, biomeColor[5]));
							break;
						case 6:
							source.SetPixel(x, y, Color.FromArgb(255, biomeColor[6]));
							break;
						default:
							source.SetPixel(x, y, Color.FromArgb(255, biomeColor[7]));
							break;
					}
				}
			}
			source.Save("test.bmp");
		}

		public static void GenerateBiomes(int depth)
		{
			/* Generation steps
			 * 
			 * - create source image
			 * - generate poisson seed points for first stage corse biome definition
			 * - define biomes for initial seed points
			 *   - forest: -y >= |x|
			 *   - plains: x > |y|
			 *   - swamp: -x > |y|
			 *   - desert: y >= |x|
			 *   - badlands: d > depth, or d < (depth >> 2)
			 * - generate voronoi from seed points
			 *   - NOTE: distance formula between points: (x2 - x1) ^ 2 + (y2 - y1) ^ 2
			 *   - NOTE: distance formula between point and circle: |(x - ox) ^ 2 + (y - oy) ^ 2 - r|
			 * - while current desired radius is > 4, recursively generate seed Poisson points fram previouse itteration, and generate Voronoi from new seeds
			 * - colorize the image
			 */
			using (var output = new Bitmap((depth << 1) + 1, (depth << 1) + 1))
			{
				var seeds = Poisson(depth, output, true);

				Voronoi(seeds, output, true, depth);

				for(int i = 1; i <= 1; i++)
				{
					var ndepth = depth >> i;
					var seed = Poisson(ndepth, output, false);

					Voronoi(seed, output, false, ndepth);
				}

				Colorize(output, true, depth);				
			}


		}


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
				int percent = 0;

				/* Poisson disk sampling
				 * variables:
				 * r: the minimal distance allowed between samples
				 * k: the number of tries each step, typically 30
				 */

				var pr = 100;
				var pk = 30;

				/* step 0:
				 * + initialize n dimentional array (grid) to store samples and speeding up spacial calculations [in this case n = 2]
				 * + set cell size to r * sqrt(n) / n [in this case n = 2]
				 * + populate array with (-1,-1)
				 */

				var pcellsize = pr * Math.Sqrt(2) / 2;
				var pcellinverse = Math.Sqrt(2) / pr;
				int psize = (int)Math.Ceiling(((depth << 1) + 1) * pcellinverse);

				var pgrid = new Point[psize, psize];
				for (int i = 0; i < psize; i++)
				{
					for (int j = 0; j < psize; j++)
					{
						pgrid[i, j] = new Point(-1, -1);
					}
				}

				/* Step 1:
				 * + pick a random point in the domain
				 * + add point to background grid (the array)
				 * + initialize active array with this starting point's index
				 */

				var sample0 = new Point((output.Width >> 1), (output.Height >> 1));

				pgrid[(int)Math.Floor(sample0.X * pcellinverse), (int)Math.Floor(sample0.Y * pcellinverse)] = sample0; // <- this will add the chosen sample into the grid at the proper location.
				var pactive = new List<Point> { sample0 };

				/* Step 2:
				 * + While active is not empty, pick a random index i from active
				 * + generate up to k points between r and 2r away from selected active sample
				 * + for each generated sample, check to see if it is within the range of r of any other sample, using the grid to narrow the search to only neighbors
				 * + if generated point is valid, add it to the active list
				 * + if no valid samples were generated, remove i from active list
				 */
				 
				var seeds = new List<Point>();

				Console.WriteLine("Starting Poisson seeding...");

				while (pactive.Count > 0)
				{
					var workingIndex = rng.Next(pactive.Count);

					var working = pactive[workingIndex];

					var foundvalid = false;

					for (int i = 0; i < pk; i++)
					{
						var sa = rng.NextDouble() * Math.PI * 2;
						var sr = (rng.NextDouble() * pr) + pr;

						var sx = sr * Math.Cos(sa) + working.X;
						var sy = sr * Math.Sin(sa) + working.Y;

						sx = Math.Max(0, sx);
						sy = Math.Max(0, sy);

						sx = Math.Min(sx, (depth << 1));
						sy = Math.Min(sy, (depth << 1));

						var sample = new Point((int)sx, (int)sy);
						var sgrid = new Point((int)Math.Floor(sample.X * pcellinverse), (int)Math.Floor(sample.Y * pcellinverse));
						
						var isValid = true;

						for (int j = -1; j <= 1; j++)
						{
							for (int k = -1; k <= 1; k++)
							{
								if ((j + sgrid.X) >= 0 && (j + sgrid.X) < psize && (k + sgrid.Y) >= 0 && (k + sgrid.Y) < psize)
								{
									var neighbor = pgrid[sgrid.X + j, sgrid.Y + k];

									if (neighbor != new Point(-1, -1))
									{
										var dist = ((neighbor.X - sample.X) * (neighbor.X - sample.X)) + ((neighbor.Y - sample.Y) * (neighbor.Y - sample.Y));
										if (dist < (pr * pr))
										{
											isValid = false;
										}
									}
								}
							}
						}

						if(isValid)
						{
							pgrid[sgrid.X, sgrid.Y] = sample;
							pactive.Add(sample);
							seeds.Add(sample);
							foundvalid = true;
						}
					}

					if (!foundvalid)
					{
						pactive.RemoveAt(workingIndex);
					}
				}

				for (int i = 0; i < seeds.Count; i++)
				{
					var Biome = biomeColor[rng.Next(8)];

					output.SetPixel(seeds[i].X, seeds[i].Y, Biome);
				}

				//check to see what seed in closest, and set the chosen pixel's color to the same color
				for (int x = 0; x < output.Width; x++)
				{
					for (int y = 0; y < output.Height; y++)
					{
						var curPoint = new Point();

						var curgrid = new Point((int)Math.Floor(x * pcellinverse), (int)Math.Floor(y* pcellinverse));

						int curDistance = int.MaxValue;

						for (int i = -2; i <= 2; i++)
						{
							for (int j = -2; j <= 2; j++)
							{
								if ((i + curgrid.X) >= 0 && (i + curgrid.X) < psize && (j + curgrid.Y) >= 0 && (j + curgrid.Y) < psize)
								{
									var neighbor = pgrid[curgrid.X + i, curgrid.Y + j];

									if (neighbor != new Point(-1, -1))
									{
										var wdist = ((neighbor.X - x) * (neighbor.X - x)) + ((neighbor.Y - y) * (neighbor.Y - y));

										if (wdist < curDistance)
										{
											curPoint = neighbor;
											curDistance = wdist;
										}
									}
								}
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

			GenerateBiomes(720);

			sw.Stop();
			Console.WriteLine("Finished generation in: " + sw.Elapsed);
			Console.ReadKey(true);
		}
	}
}
