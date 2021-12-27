using System;
using System.Collections.Generic;
using System.Linq;

namespace Lab_4
{
	public class Program
	{
		private static GenAg<bool> genetic;
		private static Random random;

		static void Main(string[] args)
		{
			random = new Random();

			Parameters.matrix = Adjutchart.GetMatrix(44, 2, 30);
			Parameters.List = Adjutchart.AdjMatrixToList(Parameters.matrix);
			Parameters.getRandomGene = () => random.NextDouble() < 0.5 ? true : false;
			Parameters.Upgrade = Upgrading;

			genetic = new GenAg<bool>(
			Parameters.Populate,
			Parameters.matrix.GetLength(0),
			random,
			Parameters.getRandomGene,
			ValidFunc,
			Parameters.elitism,
			ThirdMove,
			Parameters.Upgrade,
			Parameters.mutationRate);

			while (Update()) ;
			Console.WriteLine($"Valid: {genetic.BestIndivid.Valid}");
			int drawn = 0;
			int all = 0;
			drawn = genetic.BestIndivid.Genes.Where(n => n).Count();
			all = Parameters.List.Sum(n => n.Count);
			Console.WriteLine($"Drawn {drawn} of {all} vertices");
		}

		static bool Update()
		{
			genetic.NewGeneration();
			if (genetic.BestIndivid.Valid == 1)
				return false;
			return true;
		}

		private static float ValidFunc(TAg<bool> TAg)
		{
			float mark = 0;
			var List = Parameters.List;
			float target = 0f;

			for (int i = 0; i < List.Count; i++)
			{
				target += List[i].Count;

				if (TAg.Genes[i])
					mark += List[i].Count;
				else
				{
					for (int j = 0; j < List[i].Count; j++)
					{
						if (TAg.Genes[i] || TAg.Genes[List[i][j]])
							mark++;
					}
				}
			}
			mark /= target;

			mark = (MathF.Pow(2, mark) - 1) / (2 - 1);

			return mark;
		}

		private static TAg<bool> FirstMove(TAg<bool> firstParent, TAg<bool> otherParent)
		{
			TAg<bool> child = new TAg<bool>(
				firstParent.Genes.Length,
				random,
				Parameters.getRandomGene,
				FirstMove,
				false);

			for (int i = 0; i < firstParent.Genes.Length; i++)
			{
				child.Genes[i] = random.NextDouble() < 0.5 ? firstParent.Genes[i] : otherParent.Genes[i];
			}

			return child;
		}
		private static TAg<bool> SecondMove(TAg<bool> firstParent, TAg<bool> otherParent)
		{
			TAg<bool> child = new TAg<bool>(
					firstParent.Genes.Length,
					random,
					Parameters.getRandomGene,
					SecondMove,
					shouldInitGenes: false);

			child.Genes = firstParent.Genes[..(firstParent.Genes.Length / 2)]
				.Concat(otherParent.Genes[(firstParent.Genes.Length / 2)..])
				.ToArray();
			return child;
		}
		private static TAg<bool> ThirdMove(TAg<bool> firstParent, TAg<bool>otherParent)
		{
			TAg<bool> child = new TAg<bool>(
					firstParent.Genes.Length,
					random,
					Parameters.getRandomGene,
					ThirdMove,
					shouldInitGenes: false);

			child.Genes = firstParent.Genes[..(firstParent.Genes.Length / 3)]
.Concat(otherParent.Genes[(firstParent.Genes.Length / 3)..(2 * firstParent.Genes.Length / 3)])
				.Concat(otherParent.Genes[(2 * firstParent.Genes.Length / 3)..])
				.ToArray();
			return child;
		}

		private static void Upgrading(TAg<bool> child)
		{
			child.Genes[
			Parameters.List
			.IndexOf(
			Parameters.List.OrderByDescending(n => n.Count).First())] = true;
		}
	}

	public static class Parameters
	{
		static public int[,] matrix;
		static public List<List<int>> List;
		static public int Populate = 200;
		static public float mutationRate = 0.01f;
		static public int elitism = 5;
		static public Func<bool> getRandomGene;
		static public Func<TAg<bool>, TAg<bool>, TAg<bool>> Movefunc;
		static public Func<float, bool[]> mutationFunction;
		static public Action<TAg<bool>> Upgrade;
	}

	public static class Adjutchart
	{
		public static int[,] GetMatrix(int vertCount, int minDegree, int maxDegree)
		{
			int[,] matrix = new int[vertCount, vertCount];
			var rand = new Random();
			int degreeCount = 0,
				   matrixLength = matrix.GetLength(0);
			double randNum;


			for (int i = 0; i < matrixLength; i++)
			{
				degreeCount = 0;
				for (int j = 0; j < i; j++)
				{
					if (matrix[i, j] == 1)
						degreeCount++;
				}

				for (int j = i + 1; j < matrixLength; j++)
				{
					randNum = rand.NextDouble();
					if (randNum < 0.3)
					{
						matrix[i, j] = 1;
						matrix[j, i] = 1;
						degreeCount++;
					}
					if (degreeCount > maxDegree)
						break;
				}
				if (degreeCount < minDegree)
					i--;
			}

			return matrix;
		}

		public static List<List<int>> AdjMatrixToList(int[,] a)
		{
			int l = a.GetLength(0);
			List<List<int>> ListArray = new List<List<int>>(l);
			int i, j;

			for (i = 0; i < l; i++)
			{
				ListArray.Add(new List<int>());
				for (j = 0; j < l; j++)
				{
					if (a[i, j] == 1)
					{
						ListArray[i].Add(j);
					}
				}
			}

			return ListArray;
		}

		public static void ShowMatrix(int[,] matrix)
		{
			for (int i = 0; i < matrix.GetLength(0); i++)
			{
				for (int j = 0; j < matrix.GetLength(1); j++)
				{
					Console.Write(matrix[i, j] + " ");
				}
				Console.WriteLine();
			}
		}
	}

	public class GenAg<T>
	{
		public List<TAg<T>> Population { get; private set; }
		public int Generation { get; private set; }
		public TAg<T> BestIndivid { get; private set; }

		public int Elitism;
		public float MutationRate;

		private List<TAg<T>> newPopulation;
		private Random random;
		private float ValidSum;
		private Func<TAg<T>, float> ValidFunc;
		private Action<TAg<T>> Upgrade;

		public GenAg(
			int Populate,
			int TAgSize,
			Random random,
			Func<T> getRandomGene,
			Func<TAg<T>, float> ValidFunc,
			int elitism,
			Func<TAg<T>, TAg<T>, TAg<T>> Movefunc,
			Action<TAg<T>> Upgrade,
			float mutationRate = 0.01f)
		{
			Generation = 1;
			Elitism = elitism;
			this.Upgrade = Upgrade;
			MutationRate = mutationRate;
			Population = new List<TAg<T>>(Populate);
			newPopulation = new List<TAg<T>>(Populate);
			this.random = random;
			this.ValidFunc = ValidFunc;
			this.BestIndivid = new TAg<T>(TAgSize, random, getRandomGene, Movefunc, false);

			for (int i = 0; i < Populate; i++)
			{
				Population.Add(new TAg<T>(TAgSize, random, getRandomGene, Movefunc, shouldInitGenes: true));
			}
		}

		public void NewGeneration()
		{
			if (Population.Count <= 0)
				return;

			CalculateValid();
			Population = Population
				.OrderByDescending(n => n.Valid)
				.ToList();
			newPopulation.Clear();

			for (int i = 0; i < Population.Count; i++)
			{
				if (i < Elitism)
					newPopulation.Add(Population[i]);
				else
				{
					TAg<T> parent1 = ChooseParent();
					TAg<T> parent2 = ChooseParent();

					TAg<T> child = parent1.Move(parent1, parent2);

					child.Mutate(MutationRate);

					Upgrade(child);

					newPopulation.Add(child);
				}

			}

			List<TAg<T>> tmpList = Population;
			Population = newPopulation;
			newPopulation = tmpList;

			Generation++;
		}

		private void CalculateValid()
		{
			ValidSum = 0;
			BestIndivid = Population[0];

			for (int i = 0; i < Population.Count; i++)
			{
				Population[i].Valid = ValidFunc.Invoke(Population[i]);
				ValidSum += Population[i].Valid;

				if (Population[i].Valid > BestIndivid.Valid)
				{
					BestIndivid = Population[i];
				}
			}
		}

		private TAg<T> ChooseParent()
		{
			double randomNumber = random.NextDouble() * ValidSum;

			for (int i = 0; i < Population.Count; i++)
			{
				if (randomNumber < Population[i].Valid)
				{
					return Population[i];
				}

				randomNumber -= Population[i].Valid;
			}
			return null;
		}
	}


	public class TAg<T>
	{
		public T[] Genes { get; set; }
		public float Valid { get; set; }

		private Random random;
		private Func<T> getRandomGene;
		public Func<TAg<T>, TAg<T>, TAg<T>> Move;

		public TAg(int size,
			Random random,
			Func<T> getRandomGene,
			Func<TAg<T>, TAg<T>, TAg<T>> Move,
			bool shouldInitGenes = true)
		{
			Genes = new T[size];
			this.Move = Move;
			this.random = random;
			this.getRandomGene = getRandomGene;

			if (shouldInitGenes)
			{
				for (int i = 0; i < Genes.Length; i++)
				{
					Genes[i] = getRandomGene();
				}
			}
		}

		public void Mutate(float mutationRate)
		{
			for (int i = 0; i < Genes.Length; i++)
			{
				if (random.NextDouble() < mutationRate)
				{
					Genes[i] = getRandomGene();
				}
			}
		}
	}
}
