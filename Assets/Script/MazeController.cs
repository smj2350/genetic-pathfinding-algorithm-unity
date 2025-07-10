using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MazeController : MonoBehaviour
{
	public int[,] map = new int[100, 100];
	
	public Vector2 startPosition;
	public Vector2 endPosition;
	
	public GameObject wallPrefab;
	public GameObject exitPrefab;
	public GameObject startPrefab;
	public GameObject pathPrefab;
	
	public Genetic geneticAlgorithm;
	
	public List<int> fittestDirections;
	public List<GameObject> pathTiles;

	private const string Path = @"C:\Users\ok\Desktop\New Unity Project\Map\Tile.txt";
	private StreamReader _fp;

	private void Start ()
	{
		_fp = File.OpenText(Path);

		for (var i = 0; i < 100; i++)
		{
			for (var j = 0; j < 100; j++)
			{
				var a = _fp.Read();

				if (a == 10 || a == 13) // 엔터 만나면 리드 한번 하고 나가
				{
					_fp.Read();
					break;
				}

				switch (a)
				{
					case -1:
						a = 4; // 안보이는 벽
						break;
					case 48:
						a = 0; // 땅
						break;
					case 49:
						a = 1; // 보이는 벽
						break;
					case 50:
						a = 2; // 시작점
						break;
					case 51:
						a = 3; // 도착점
						break;
				}
				
				map[i, j] = a;
			}
		}
 
		_fp.Close();
		
		// map = new int[,] 
		// {
		// 	{1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
		// 	{1,0,1,0,0,0,0,0,1,1,1,0,0,0,1},
		// 	{1,0,0,0,0,0,0,0,1,1,1,0,0,0,1},
		// 	{1,0,0,0,1,1,1,0,0,1,0,0,0,0,1},
		// 	{1,0,0,0,1,1,1,0,0,0,0,0,1,0,1},
		// 	{1,1,0,0,1,1,1,0,0,0,0,0,1,0,1},
		// 	{1,0,0,0,0,1,0,0,0,0,1,1,1,0,1},
		// 	{1,0,1,1,0,0,0,1,0,0,0,0,0,0,2},
		// 	{3,0,1,1,0,0,0,1,0,0,0,0,0,0,1},
		// 	{1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}
		// };
		
		Populate ();
		
		fittestDirections = new List<int> ();
		pathTiles = new List<GameObject> ();

		geneticAlgorithm = new Genetic { mazeController = this};
		geneticAlgorithm.Run ();
	}
	
	private void Update () 
	{
		if (geneticAlgorithm.busy) geneticAlgorithm.Epoch ();
		
		RenderFittestChromosomePath ();
		
		var lastPosition = pathTiles.Last ().transform.position;
		DisplayManager.Instance.generation.text = "세대 : " + geneticAlgorithm.generation + " (X : " + lastPosition.x + ", Y : " + lastPosition.z + ")";
	}
	
	private GameObject PrefabByTile(int tile) 
	{
		switch (tile)
		{
			case 1:
				return wallPrefab;
			case 2:
				return startPrefab;
			case 3:
				return exitPrefab;
			default:
				return null;
		}
	}

	private Vector2 Move(Vector2 position, int direction) 
	{
		switch (direction) 
		{
		case 0: // 위
			if (position.y - 1 < 0 || map [(int)(position.y - 1), (int)position.x] == 1) { }
			else
			{
				position.y -= 1;
			}
			
			break;
		case 1: // 아
			if (position.y + 1 >= map.GetLength (0) || map [(int)(position.y + 1), (int)position.x] == 1) { }
			else 
			{
				position.y += 1;
			}
			
			break;
		case 2: // 오
			if (position.x + 1 >= map.GetLength (1) || map [(int)position.y, (int)(position.x + 1)] == 1) { }
			else 
			{
				position.x += 1;
			}
			
			break;
		case 3: // 왼
			if (position.x - 1 < 0 || map [(int)position.y, (int)(position.x - 1)] == 1) { }
			else
			{
				position.x -= 1;
			}

			break;
		}
		return position;
	}

	public double TestRoute(IEnumerable<int> directions)
	{
		var position = directions.Aggregate(startPosition, (current, nextDirection) => Move(current, nextDirection));

		var deltaPosition = new Vector2(Math.Abs(position.x - endPosition.x), Math.Abs(position.y - endPosition.y));
		
		var result = 1 / (double)(deltaPosition.x + deltaPosition.y + 1);
		
		if (result == 1)
			Debug.Log ("테스트 결과 = " + result + ",("+position.x+","+position.y+")");
		
		return result;
	}

	public void Populate() // 채우기
	{
		Debug.Log ("length 0 =" + map.GetLength(0));
		Debug.Log ("length 1 =" + map.GetLength(1));

		for (var y = 0; y < map.GetLength(0); y++) 
		{
			for (var x = 0; x < map.GetLength(1); x++)
			{
				if (map[y, x] == 2)
				{
					startPosition = new Vector2 (x, y);
				}

				if (map[y, x] == 3)
				{
					endPosition = new Vector2 (x, y);
				}

				var prefab = PrefabByTile (map [y, x]);
				if (prefab == null) continue;
				var wall = Instantiate (prefab);
				wall.transform.position = new Vector3 (x, 0, -y);
			}
		}
	}
	
	public void ClearPathTiles() 
	{
		foreach (var pathTile in pathTiles) {
			Destroy(pathTile);
		}
		pathTiles.Clear();
	}

	public void RenderFittestChromosomePath() 
	{
		ClearPathTiles ();
		var fittestGenome = geneticAlgorithm.genomes[geneticAlgorithm.fittestGenome];
		var fittestDirections = Genetic.Decode (fittestGenome.bits);
		var position = startPosition;

		foreach (var direction in fittestDirections)
		{
			position = Move (position, direction);
			var pathTile = Instantiate (pathPrefab);
			pathTile.transform.position = new Vector3(position.x, 0, -position.y);
			pathTiles.Add (pathTile);
		}
	}
}
