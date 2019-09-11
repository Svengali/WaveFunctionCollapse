/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Collections.Generic;


public class PQNode : Priority_Queue.FastPriorityQueueNode
{
	public int i;

	public bool removed = false;

};


abstract class Model
{
	protected bool[][] wave;

	protected int[][][] propagator;
	int[][][] compatible;
	protected int[] observed;

	List<(int, int)> stack = new List<(int, int)>();

	//(int, int)[] stack;
	int stacksize;

	LinkedList<(int i, int t, int[] comp)> m_bans = new LinkedList<(int i, int t, int[])>();

	protected Random random;
	protected int FMX, FMY, T;
	protected bool periodic;

	protected double[] weights;
	double[] weightLogWeights;

	int[] sumsOfOnes;
	double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
	double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

	Priority_Queue.FastPriorityQueue<PQNode> m_pq;
	PQNode[] m_nodes;

	protected Model( int width, int height )
	{
		FMX = width;
		FMY = height;
	}

	void Init()
	{
		wave = new bool[FMX * FMY][];
		compatible = new int[wave.Length][][];
		for( int i = 0; i < wave.Length; i++ )
		{
			wave[i] = new bool[T];
			compatible[i] = new int[T][];
			for( int t = 0; t < T; t++ )
				compatible[i][t] = new int[4];
		}

		weightLogWeights = new double[T];
		sumOfWeights = 0;
		sumOfWeightLogWeights = 0;

		for( int t = 0; t < T; t++ )
		{
			weightLogWeights[t] = weights[t] * Math.Log( weights[t] );
			sumOfWeights += weights[t];
			sumOfWeightLogWeights += weightLogWeights[t];
		}

		startingEntropy = Math.Log( sumOfWeights ) - sumOfWeightLogWeights / sumOfWeights;

		sumsOfOnes = new int[FMX * FMY];
		sumsOfWeights = new double[FMX * FMY];
		sumsOfWeightLogWeights = new double[FMX * FMY];
		entropies = new double[FMX * FMY];

		//stack = new (int, int)[wave.Length * T];
		stacksize = 0;



	}

	bool? Observe()
	{
		//double min = 1E+3;
		int argmin = -1;

		/*
		for( int i = 0; i < wave.Length; i++ )
		{
			if( OnBoundary( i % FMX, i / FMX ) )
				continue;

			int amount = sumsOfOnes[i];
			if( amount == 0 )
				return false;

			double entropy = entropies[i];
			if( amount > 1 && entropy <= min )
			{
				double noise = 1E-6 * random.NextDouble();
				if( entropy + noise < min )
				{
					min = entropy + noise;
					argmin = i;
				}
			}
		}
		*/

		if( m_pq.Count == 0 ) 
			return true;

		var node = m_pq.First;

		argmin = node.i;

		if( m_pq.Count % 5000 == 0 )
			Console.WriteLine( $"Nodes left = {m_pq.Count}" );

		if( argmin == -1 )
		{
			observed = new int[FMX * FMY];
			for( int i = 0; i < wave.Length; i++ )
				for( int t = 0; t < T; t++ )
					if( wave[i][t] )
					{ observed[i] = t; break; }
			return true;
		}

		double[] distribution = new double[T];
		for( int t = 0; t < T; t++ )
			distribution[t] = wave[argmin][t] ? weights[t] : 0;
		int r = distribution.Random(random.NextDouble());

		bool[] w = wave[argmin];
		for( int t = 0; t < T; t++ )
		{
			if( w[t] != ( t == r ) )
			{
				var count = Ban( argmin, t );

				if( count == 0 )
				{
					var last = m_bans.Last;

					Unban( last.Value.i, last.Value.t, last.Value.comp );
				}
			}
		}

		return null;
	}

	static bool s_unban = false;

	protected void Propagate()
	{
		while( stack.Count > 0 )
		{
			var e1 = stack[stack.Count - 1];
			stack.RemoveAt( stack.Count - 1 );

			int i1 = e1.Item1;
			int x1 = i1 % FMX, y1 = i1 / FMX;

			for( int d = 0; d < 4; d++ )
			{
				int dx = DX[d], dy = DY[d];
				int x2 = x1 + dx, y2 = y1 + dy;
				if( OnBoundary( x2, y2 ) )
					continue;

				if( x2 < 0 )
					x2 += FMX;
				else if( x2 >= FMX )
					x2 -= FMX;
				if( y2 < 0 )
					y2 += FMY;
				else if( y2 >= FMY )
					y2 -= FMY;

				int i2 = x2 + y2 * FMX;
				int[] p = propagator[d][e1.Item2];
				int[][] compat = compatible[i2];

				for( int l = 0; l < p.Length; l++ )
				{
					int t2 = p[l];
					int[] comp = compat[t2];

					comp[d]--;
					if( comp[d] == 0 )
					{
						var count = Ban( i2, t2 );

						if( count == 0 )
						{
							var last = m_bans.Last.Value;

							Unban( last.i, last.t, last.comp );
						}
					}
				}
			}
		}
	}

	public bool Run( int seed, int limit )
	{
		if( wave == null )
			Init();

		Clear();
		random = new Random( seed );

		for( int l = 0; l < limit || limit == 0; l++ )
		{
			bool? result = Observe();
			if( result != null )
				return (bool)result;
			Propagate();
		}

		return true;
	}

	protected int Ban( int i, int t )
	{

		if( m_nodes[i].removed ) 
			return sumsOfOnes[i];

		int[] comp = compatible[i][t];

		var compClone = (int[])comp.Clone();

		m_bans.AddFirst( (i, t, compClone) );

		if( m_bans.Count > 16 ) m_bans.RemoveFirst();

		wave[i][t] = false;

		for( int d = 0; d < 4; d++ )
			comp[d] = 0;
		stack.Add( (i, t) );

		sumsOfOnes[i] -= 1;
		sumsOfWeights[i] -= weights[t];
		sumsOfWeightLogWeights[i] -= weightLogWeights[t];

		double sum = sumsOfWeights[i];
		entropies[i] = Math.Log( sum ) - sumsOfWeightLogWeights[i] / sum;

		if( sumsOfOnes[i] > 1 )
		{
			m_pq.UpdatePriority( m_nodes[i], (float)entropies[i] );
		}
		else
		{
			m_pq.Remove( m_nodes[i] );

			m_nodes[i].removed = true;

			//Console.WriteLine( $"Removing node {i}" );

		}

		return sumsOfOnes[i];
	}

	protected void Unban( int i, int t, int[] oldComp )
	{
		wave[i][t] = true;

		compatible[i][t] = oldComp;

		stacksize--;

		sumsOfOnes[i] += 1;
		sumsOfWeights[i] += weights[t];
		sumsOfWeightLogWeights[i] += weightLogWeights[t];

		double sum = sumsOfWeights[i];
		entropies[i] = Math.Log( sum ) - sumsOfWeightLogWeights[i] / sum;

		if( sumsOfOnes[i] > 1 )
		{
			m_pq.UpdatePriority( m_nodes[i], (float)entropies[i] );
		}
		else
		{ 
			m_pq.Enqueue( m_nodes[i], (float)entropies[i] );
		}

	}

	protected virtual void Clear()
	{
		m_nodes = new PQNode[wave.Length];
		m_pq = new Priority_Queue.FastPriorityQueue<PQNode>( wave.Length + 10 );

		for( int i = 0; i < wave.Length; i++ )
		{
			for( int t = 0; t < T; t++ )
			{
				wave[i][t] = true;
				for( int d = 0; d < 4; d++ )
					compatible[i][t][d] = propagator[opposite[d]][t].Length;
			}

			sumsOfOnes[i] = weights.Length;
			sumsOfWeights[i] = sumOfWeights;
			sumsOfWeightLogWeights[i] = sumOfWeightLogWeights;
			entropies[i] = startingEntropy;



			//if( OnBoundary( i % FMX, i / FMX ) )
			//	continue;

			var q = new PQNode();
			q.i = i;

			m_nodes[i] = q;

			m_pq.Enqueue( q, (float)entropies[i] );

		}


	}



protected abstract bool OnBoundary( int x, int y );
public abstract System.Drawing.Bitmap Graphics();

protected static int[] DX = { -1, 0, 1, 0 };
protected static int[] DY = { 0, 1, 0, -1 };
static int[] opposite = { 2, 3, 0, 1 };
}
