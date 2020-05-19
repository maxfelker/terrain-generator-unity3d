using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrainGenerator
{
    public class TerrainChunkGenerator : MonoBehaviour
    {
        public Material TerrainMaterial;

        public Texture2D FlatTexture;
        public Texture2D SteepTexture;

        private int heightMapResolution = 129;
        private int alphaMapResolution = 129;

        public int generationRadius = 4;

        public int terrainChunkLength = 100;
        public int terrainChunkHeight = 40;

		public float perlinHeight = 1.5F;
		public float perlinDamper = 0.25F;

        private TerrainChunkSettings Settings;

        private NoiseProvider NoiseProvider;

        private ChunkCache Cache;


        private void Awake()
        {
            this.name = "TerrainGenerator";   // Enforce name to collect chunks later
            Settings = new TerrainChunkSettings(
                heightMapResolution, 
                alphaMapResolution, 
                terrainChunkLength, 
                terrainChunkHeight, 
                FlatTexture, 
                SteepTexture, 
                TerrainMaterial
            );
            Cache = new ChunkCache();
        }

        private void Update()
        {
			Cache.Update();
        }

        private void GenerateChunk(int x, int z)
        {
            if (Cache.ChunkCanBeAdded(x, z))
            {
				NoiseProvider = new NoiseProvider(perlinHeight, perlinDamper);
				var chunk = new TerrainChunk(Settings, NoiseProvider, x, z);
                Cache.AddNewChunk(chunk);
            }
        }

        private void RemoveChunk(int x, int z)
        {
            if (Cache.ChunkCanBeRemoved(x, z))
                Cache.RemoveChunk(x, z);
        }

        private List<Vector2i> GetChunkPositionsInRadius(Vector2i chunkPosition, int radius)
        {
            var result = new List<Vector2i>();

            for (var zCircle = -radius; zCircle <= radius; zCircle++)
            {
                for (var xCircle = -radius; xCircle <= radius; xCircle++)
                {
                    if (xCircle * xCircle + zCircle * zCircle < radius * radius)
                        result.Add(new Vector2i(chunkPosition.X + xCircle, chunkPosition.Z + zCircle));
                }
            }

            return result;
        }

        public void UpdateTerrain(Vector3 worldPosition)
        {
            var chunkPosition = GetChunkPosition(worldPosition);
            var newPositions = GetChunkPositionsInRadius(chunkPosition, generationRadius);

            var loadedChunks = Cache.GetGeneratedChunks();
            var chunksToRemove = loadedChunks.Except(newPositions).ToList();

            var positionsToGenerate = newPositions.Except(chunksToRemove).ToList();
            foreach (var position in positionsToGenerate)
                GenerateChunk(position.X, position.Z);
        }

        public Vector2i GetChunkPosition(Vector3 worldPosition)
        {
            var x = (int)Mathf.Floor(worldPosition.x / Settings.Length);
            var z = (int)Mathf.Floor(worldPosition.z / Settings.Length);

            return new Vector2i(x, z);
        }

        public bool IsTerrainAvailable(Vector3 worldPosition)
        {
            var chunkPosition = GetChunkPosition(worldPosition);
            return Cache.IsChunkGenerated(chunkPosition);
        }

        public float GetTerrainHeight(Vector3 worldPosition)
        {
            var chunkPosition = GetChunkPosition(worldPosition);
            var chunk = Cache.GetGeneratedChunk(chunkPosition);
            if (chunkPosition != null)
                return chunk.GetTerrainHeight(worldPosition);

            return 0;
        }
    }
}