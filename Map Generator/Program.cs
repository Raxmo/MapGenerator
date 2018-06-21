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
												 Color.FromArgb(2 << 5, Color.Olive),
												 Color.FromArgb(3 << 5, Color.DodgerBlue),
												 Color.FromArgb(4 << 5, Color.ForestGreen),
												 Color.FromArgb(5 << 5, Color.AntiqueWhite),
												 Color.FromArgb(6 << 5, Color.Yellow),
												 Color.FromArgb(7 << 5, Color.Firebrick)};



		public static void GenerateChunks(Bitmap biomeData)
		{
			/* Steps for chunk creation
			 * - pull biome data from file world.bmp
			 * - for every pixel in biome data, create a chunk
			 * - when creating a chunk, for every pixel in the chunk, determin which chunk seed it is closest to and assign same biome to target pixel
			 * - use voronoi recursion to define borders between biomes?
			 * - colorize by adding a little static to every pixel.
			 */

			var progress = 0;
			var total = biomeData.Width * biomeData.Height;

			Console.WriteLine("Generating chunks...");

			for (int x = 0; x < biomeData.Width; x++)
			{
				for (int y = 0; y < biomeData.Height; y++)
				{
					using (Bitmap output = new Bitmap(256, 256))
					{
						var seedloc = new Point(biomeData.GetPixel(x, y).R, biomeData.GetPixel(x, y).G);
						var seedBiome = biomeData.GetPixel(x, y).A;

						var neighbors = new List<Point> { };
						var neighborBiomes = new List<byte> { };

						for(int jx = -1; jx <= 1; jx++)
						{
							for(int jy = -1; jy <= 1; jy++)
							{
								if(jx + x >= 0 && jx + x < biomeData.Width && jy + y >= 0 && jy + y < biomeData.Height)
								{
									neighbors.Add(new Point(biomeData.GetPixel(jx + x, jy + y).R + (jx * output.Width), biomeData.GetPixel(jx + x, jy + y).G + (jy * output.Height)));
									neighborBiomes.Add(biomeData.GetPixel(jx + x, jy + y).A);
								}
							}
						}

						for (int ix = 0; ix < output.Width; ix++)
						{
							for (int iy = 0; iy < output.Height; iy++)
							{
								var curDist = int.MaxValue;
								var foundIndex = 0;

								for(int k = 0; k < neighbors.Count; k++)
								{
									var neighbor = neighbors[k];
									var wdist = ((neighbor.X - ix) * (neighbor.X - ix)) + ((neighbor.Y - iy) * (neighbor.Y - iy));

									if (wdist < curDist)
									{
										curDist = wdist;
										foundIndex = k;
									}
								}

								output.SetPixel(ix, iy, Color.FromArgb(neighborBiomes[foundIndex], Color.White));
							}
						}
						output.Save("chunk," + x + "," + y + ".bmp");
					}
				}
				progress = (100 * x) / biomeData.Width;
				Console.Write("\r progress: {0}%      ", progress);
			}
			Console.WriteLine("\r Complete            ");
		}

		public static void SeedChunks(Bitmap source)
		{
			Console.WriteLine("Seeding chunks...");
			var progress = 0;

			for(int x = 0; x < source.Width; x++)
			{
				for (int y = 0; y < source.Height; y++)
				{
					source.SetPixel(x, y, Color.FromArgb(source.GetPixel(x, y).A, (64 + rng.Next(128)), (64 + rng.Next(128)), 0));
				}

				progress = (100 * x) / source.Width;
				Console.Write("\r progress: {0}%      ", progress);
			}
			Console.WriteLine("\r Complete     ");
		}

		public static Point[,] Poisson(int depth, Bitmap source, bool isFirstItteration)
		{
			Console.WriteLine("Initiallizing Poisson generation...");

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
				Console.Write("\r {0} points left to handle...            ", active.Count);

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

					sx = Math.Min(sx, source.Width - 1);
					sy = Math.Min(sy, source.Height - 1);


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
							if(rng.NextDouble() < 0.01
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.badlands] 
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.ruins] 
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.mountain]
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.river])
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, biomeColor[(int)biome.ruins]);
							}
							else if(rng.NextDouble() < 0.01
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.badlands]
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.ruins]
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.mountain]
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.river])
							{
								source.SetPixel(grid[i, j].X, grid[i, j].Y, biomeColor[(int)biome.mountain]);
							}
							else if (rng.NextDouble() < 0.01
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.badlands]
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.ruins]
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.mountain]
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.river]
								&& source.GetPixel(grid[i, j].X, grid[i, j].Y) != biomeColor[(int)biome.desert])
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

			Console.WriteLine("\r Complete                      ");

			return grid;
		}

		public static void Voronoi(Point[,] seeds, Bitmap source, bool isFirstItteration, int depth)
		{
			var progress = 0;

			Console.WriteLine("Initializing Voronoi generation...");

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
						var odist = depth << 1;
						var ndist = Math.Abs(((x - o.X) * (x - o.X)) + ((y - o.Y) * (y - o.Y)) - (odist * odist));
						if(ndist < curDistance)
						{
							curPoint = new Point(source.Width >> 1, source.Height >> 1);
						}
					}
					source.SetPixel(x, y, source.GetPixel(curPoint.X, curPoint.Y));
				}
				progress = (int)((x * 100) / source.Width);

				Console.Write("\r Progress: {0}    ", progress);
			}
			Console.WriteLine("\r Complete             ");

		}

		public static void Colorize(Bitmap source, bool isFirstItteration, int depth)
		{
			var progress = 0;

			if(!isFirstItteration)
			{
				Console.WriteLine("Initializing coloration...");
			}
			else
			{
				Console.WriteLine("Backing biome information...");
			}

			for (int x = 0; x < source.Width; x++)
			{
				for (int y = 0; y < source.Height; y++)
				{
					if (isFirstItteration)
					{
						var o = new Point(source.Width >> 1, source.Height >> 1);
						var odist = depth << 1;
						var ndist = Math.Abs(((x - o.X) * (x - o.X)) + ((y - o.Y) * (y - o.Y)) - (odist * odist));
						var dist = ((x - o.X) * (x - o.X)) + ((y - o.Y) * (y - o.Y));
						if (dist > (odist * odist))
						{
							source.SetPixel(x, y, biomeColor[(int)biome.badlands]);
						}
					}
					else
					{
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

				if (!isFirstItteration)
				{
					progress = (int)((x * 100) / source.Width);

					Console.Write("\r Progress: {0}    ", progress);
				}
			}
			source.Save("world.bmp");
			if (!isFirstItteration)
			{
				Console.WriteLine("\r Complete              ");
			}
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
			using (var output = new Bitmap((depth << 2) + depth + 1, (depth << 2) + depth + 1))
			{
				var seeds = Poisson(depth, output, true);

				Voronoi(seeds, output, true, depth);

				Colorize(output, true, depth);

				var ndepth = depth >> 1;
				while(ndepth >= 4)
				{
					seeds = Poisson(ndepth, output, false);
					Voronoi(seeds, output, false, ndepth);

					output.Save("world.bmp");

					ndepth = ndepth >> 1;
				}

				SeedChunks(output);
				output.Save("world.bmp");
			}


		}

		static void Main(string[] args)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			Console.WriteLine("Starting generation, please wait, will take some time.");

			GenerateBiomes(4);
			using (var source = new Bitmap("world.bmp"))
			{
				GenerateChunks(source);
			}

			sw.Stop();
			Console.WriteLine("Finished generation in: " + sw.Elapsed);
			Console.ReadKey(true);
		}
	}
}
