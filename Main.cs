/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Xml.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

static class Program
{

	static int attempts = 1;


	static void Main()
	{
		Stopwatch sw = Stopwatch.StartNew();

		Random random = new Random();
		XDocument xdoc = XDocument.Load("samples.xml");

		int masterCounter = 0;
		foreach (XElement xelem in xdoc.Root.Elements("overlapping", "simpletiled"))
		{


			var screenshotCount = xelem.Get("screenshots", 10);

			for (int i = 0; i < screenshotCount; i++)
			{
				Parallel.For( 0, attempts, (k) => {
					Model model;
					string name = xelem.Get<string>("name");
					Console.WriteLine( $"< {name}" );

					if( xelem.Name == "overlapping" )
						model = new OverlappingModel( name, xelem.Get( "N", 2 ), xelem.Get( "width", 48 ), xelem.Get( "height", 48 ),
xelem.Get( "periodicInput", true ), xelem.Get( "periodic", false ), xelem.Get( "symmetry", 8 ), xelem.Get( "ground", 0 ) );
					else if( xelem.Name == "simpletiled" )
						model = new SimpleTiledModel( name, xelem.Get<string>( "subset" ),
xelem.Get( "width", 10 ), xelem.Get( "height", 10 ), xelem.Get( "periodic", false ), xelem.Get( "black", false ) );
					else
						return;

					while( masterCounter < screenshotCount )
					{
						Console.Write( "> " );
						int seed = random.Next();
						bool finished = model.Run(seed, xelem.Get("limit", 0));
						if( finished )
						{
							Console.WriteLine( "DONE" );

							var counter = Interlocked.Exchange( ref masterCounter, masterCounter + 1 );

							model.Graphics().Save( $"{counter} {name} {i}.png" );
							if( model is SimpleTiledModel && xelem.Get( "textOutput", false ) )
								System.IO.File.WriteAllText( $"{counter} {name} {i}.txt", ( model as SimpleTiledModel ).TextOutput() );

							return;
						}
						else
						{
							//Console.WriteLine( $"CONTRADICTION Completed {model.CompletedNodes}" );
						}
					}

				} );


			}

		}

		Console.WriteLine($"time = {sw.ElapsedMilliseconds}");
	}
}
