using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 
/// </summary>
public class MapGenerator : MonoBehaviour
{
    public Transform tilePrefab;
    //public Vector2 mapSize;
    public Vector2 maxMapSize;
    [Range(0,1)]
    public float outlinePercent;

    public Map[] maps;
    public int mapIndex;
    

    public float tileSize;
    public Transform navmeshFloor;
    public Transform mapFloor;
    
    public Transform navmeshMaskPrefab;
    
    public Transform obstaclePrefab;

    public List<Coord> allTileCoords;
    private Queue<Coord> shuffledTileCoords;
    private Queue<Coord> shuffledOpenTileCoords;

    public Map currentMap;
    private Transform[,] tileMap;
    
    private void Awake()
    {
        FindObjectOfType<Spawner>().OnNewWave += OnNewWave;
    }

    void OnNewWave(int waveNumber)
    {
        mapIndex = waveNumber - 1;
        GenerateMap();
    }
    
    public void GenerateMap()
    {
        currentMap = maps[mapIndex];
        tileMap = new Transform[currentMap.mapSize.x, currentMap.mapSize.y];
        System.Random prng = new System.Random(currentMap.seed);
        
        //generating coords
        allTileCoords = new List<Coord>();
        for (int i = 0; i < currentMap.mapSize.x; i++)
        {
            for (int j = 0; j < currentMap.mapSize.y; j++)
            {
                allTileCoords.Add(new Coord(i,j));
            }
        }

        shuffledTileCoords = new Queue<Coord>(Utility.ShuffleArray(allTileCoords.ToArray(),currentMap.seed));

        //create mapHolder object
        string holdName = "Generated Map";
        if (transform.Find(holdName))
        {
            DestroyImmediate(transform.Find(holdName).gameObject);
        }

        Transform mapHolder = new GameObject(holdName).transform;
        mapHolder.parent = transform;
        
        //spawning tiles
        for (int i = 0; i < currentMap.mapSize.x; i++)
        {
            for (int j = 0; j < currentMap.mapSize.y; j++)
            {
                Vector3 tilePosition = CoordToPosition(i, j);
                Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(Vector3.right * 90)) as Transform;
                newTile.localScale = Vector3.one * (1 - outlinePercent) * tileSize;
                newTile.parent = mapHolder;
                tileMap[i, j] = newTile;
            }
        }
        
        //spawning obstacles
        bool[,] obstacleMap = new bool[(int)currentMap.mapSize.x, (int)currentMap.mapSize.y];

        int obstacleCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y * currentMap.obstaclePercent);
        int currentObstacleCount = 0;
        List<Coord> allOpenCoords = new List<Coord>(allTileCoords);

        for (int i = 0; i < obstacleCount; i++)
        {
            Coord randomCoord = GetRandomCoord();
            obstacleMap[randomCoord.x, randomCoord.y] = true;
            currentObstacleCount++;
            
            if (randomCoord!=currentMap.mapCentre && MapIsFullyAccessible(obstacleMap,currentObstacleCount))
            {
                float obstacleHeight =
                    Mathf.Lerp(currentMap.minObstacleHeight, currentMap.maxObstacleHeight, (float)prng.NextDouble());
                
                Vector3 obstaclePosition = CoordToPosition(randomCoord.x, randomCoord.y); 
                Transform newObstacle = Instantiate(obstaclePrefab, obstaclePosition + Vector3.up * obstacleHeight/2, Quaternion.identity) as Transform;
                newObstacle.parent = mapHolder;
                newObstacle.localScale = new Vector3((1 - outlinePercent) * tileSize, obstacleHeight,
                    (1 - outlinePercent) * tileSize);
                Renderer obstacleRenderer = newObstacle.GetComponent<Renderer>();
                Material obstacleMaterial = new Material(obstacleRenderer.sharedMaterial);
                float colorPercent = randomCoord.y / (float)currentMap.mapSize.y;
                obstacleMaterial.color =
                    Color.Lerp(currentMap.foregroundColor, currentMap.backgroundColor, colorPercent);
                
                obstacleRenderer.sharedMaterial = obstacleMaterial;
                allOpenCoords.Remove(randomCoord);

            }
            else
            {
                obstacleMap[randomCoord.x, randomCoord.y] = false;
                currentObstacleCount--;
            }
        }
        shuffledOpenTileCoords = new Queue<Coord>(Utility.ShuffleArray(allOpenCoords.ToArray(),currentMap.seed));
        
        //creating navmesh mask
        Transform maskLeft = Instantiate(navmeshMaskPrefab, Vector3.left*(currentMap.mapSize.x+maxMapSize.x)/4f*tileSize,Quaternion.identity) as Transform;
        maskLeft.parent = mapHolder;
        maskLeft.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x) / 2f, 1, currentMap.mapSize.y) * tileSize;
        
        Transform maskRight = Instantiate(navmeshMaskPrefab, Vector3.right*(currentMap.mapSize.x+maxMapSize.x)/4f*tileSize,Quaternion.identity) as Transform;
        maskRight.parent = mapHolder;
        maskRight.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x) / 2f, 1, currentMap.mapSize.y) * tileSize;
        
        Transform maskTop = Instantiate(navmeshMaskPrefab, Vector3.forward*(currentMap.mapSize.y+maxMapSize.y)/4f*tileSize,Quaternion.identity) as Transform;
        maskTop.parent = mapHolder;
        maskTop.localScale = new Vector3(maxMapSize.x, 1, (maxMapSize.y-currentMap.mapSize.y)/2f) * tileSize;
        
        Transform maskBottom = Instantiate(navmeshMaskPrefab, Vector3.back*(currentMap.mapSize.y+maxMapSize.y)/4f*tileSize,Quaternion.identity) as Transform;
        maskBottom.parent = mapHolder;
        maskBottom.localScale = new Vector3(maxMapSize.x, 1, (maxMapSize.y-currentMap.mapSize.y)/2f) * tileSize;
        
        
        
        navmeshFloor.localScale = new Vector3(maxMapSize.x, maxMapSize.y) * tileSize;
        mapFloor.localScale = new Vector3(currentMap.mapSize.x * tileSize,  currentMap.mapSize.y * tileSize);
    }

    

    bool MapIsFullyAccessible(bool[,] obstacleMap, int currentObstacleCount)
    {
        bool[,] mapFlags = new bool[obstacleMap.GetLength(0), obstacleMap.GetLength(1)];
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(currentMap.mapCentre);
        mapFlags[currentMap.mapCentre.x, currentMap.mapCentre.y] = true;

        int accessibleTileCount = 1;
        
        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int neighbourX = tile.x + i;
                    int neighbourY = tile.y + j;
                    if (i==0 || j==0)
                    {
                        if (neighbourX >= 0 && neighbourX < obstacleMap.GetLength(0) && neighbourY >= 0 && neighbourY < obstacleMap.GetLength(1))
                        {
                            if (!mapFlags[neighbourX,neighbourY] && !obstacleMap[neighbourX,neighbourY])
                            {
                                mapFlags[neighbourX, neighbourY] = true;
                                queue.Enqueue(new Coord(neighbourX,neighbourY));
                                accessibleTileCount++;
                            }
                        }
                    }
                }
            }
        }

        int targetAccessibleTileCount = (int) (currentMap.mapSize.x * currentMap.mapSize.y - currentObstacleCount);

        return targetAccessibleTileCount == accessibleTileCount;
    }

    public Transform GetTileFromPosition(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / tileSize + (currentMap.mapSize.x - 1) / 2f);
        int y = Mathf.RoundToInt(position.z / tileSize + (currentMap.mapSize.y - 1) / 2f);
        x = Mathf.Clamp(x, 0, tileMap.GetLength(0) - 1);
        y = Mathf.Clamp(y, 0, tileMap.GetLength(1)-1);
        return tileMap[x, y];
    }
    Vector3 CoordToPosition(int i, int j)
    {
        return new Vector3(-currentMap.mapSize.x / 2f + 0.5f + i, 0, -currentMap.mapSize.y/2f + 0.5f + j) * tileSize;
    }

    public Coord GetRandomCoord()
    {
        Coord randomCoord = shuffledTileCoords.Dequeue();
        shuffledTileCoords.Enqueue(randomCoord);
        return randomCoord;
    }

    public Transform GetRandomOpenTile() 
    {
        Coord randomCoord = shuffledOpenTileCoords.Dequeue();
        shuffledOpenTileCoords.Enqueue(randomCoord);
        return tileMap[randomCoord.x,randomCoord.y];
    }
    
    [System.Serializable]
    public struct Coord
    {
        public int x;
        public int y;

        public Coord(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public static bool operator ==(Coord c1, Coord c2)
        {
            return c1.x == c2.x && c1.y == c2.y;
        }
        public static bool operator !=(Coord c1, Coord c2)
        {
            return !(c1 == c2);
        }
    }
    [System.Serializable]
    public class Map
    {
        public Coord mapSize;
        [Range(0,1)]
        public float obstaclePercent;
        public int seed;
        public float minObstacleHeight;
        public float maxObstacleHeight;
        public Color foregroundColor;
        public Color backgroundColor;

        public Coord mapCentre
        {
            get
            {
                return new Coord(mapSize.x / 2, mapSize.y / 2);
            }
        }
        

    }

}
