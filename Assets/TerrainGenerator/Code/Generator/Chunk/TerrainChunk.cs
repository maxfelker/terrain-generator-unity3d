﻿using System.Threading;
using UnityEngine;

namespace TerrainGenerator
{
    public class TerrainChunk
    {
        public Vector2i Position { get; private set; }

        private Terrain Terrain { get; set; }

        private TerrainData Data { get; set; }

        private TerrainChunkSettings Settings { get; set; }

        private NoiseProvider NoiseProvider { get; set; }

        private TerrainChunkNeighborhood Neighborhood { get; set; }

        private float[,] Heightmap { get; set; }

        private object HeightmapThreadLockObject { get; set; }

        private GameObject TerrainRoot;

        public TerrainChunk(TerrainChunkSettings settings, NoiseProvider noiseProvider, int x, int z)
        {
            HeightmapThreadLockObject = new object();
            TerrainRoot = GameObject.Find("TerrainGenerator");
            Settings = settings;
            NoiseProvider = noiseProvider;
            Neighborhood = new TerrainChunkNeighborhood();
            Position = new Vector2i(x, z);
        }

        #region Heightmap stuff

        public void GenerateHeightmap()
        {
            var thread = new Thread(GenerateHeightmapThread);
            thread.Start();
        }

        private void GenerateHeightmapThread()
        {
            lock (HeightmapThreadLockObject)
            {
                var heightmap = new float[Settings.HeightmapResolution, Settings.HeightmapResolution];

                for (var zRes = 0; zRes < Settings.HeightmapResolution; zRes++)
                {
                    for (var xRes = 0; xRes < Settings.HeightmapResolution; xRes++)
                    {
                        var xCoordinate = Position.X + (float)xRes / (Settings.HeightmapResolution - 1);
                        var zCoordinate = Position.Z + (float)zRes / (Settings.HeightmapResolution - 1);

                        heightmap[zRes, xRes] = NoiseProvider.GetValue(xCoordinate, zCoordinate);
                    }
                }

                Heightmap = heightmap;
            }
        }

        public bool IsHeightmapReady()
        {
            return Terrain == null && Heightmap != null;
        }

        public float GetTerrainHeight(Vector3 worldPosition)
        {
            return Terrain.SampleHeight(worldPosition);
        }

        #endregion

        #region Main terrain generation

        // Creates the terrain object based on settings
        public void CreateTerrain()
        {
            TerrainData data = GenerateTerrainData();            
            Terrain = GenerateTerrain(data);
            Terrain.heightmapPixelError = 8;
            Terrain.materialType = UnityEngine.Terrain.MaterialType.Custom;
            Terrain.materialTemplate = Settings.TerrainMaterial;
            Terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            Terrain.Flush();
        }

        // Creates TerrainData config based on settings supplied via inspector
        private TerrainData GenerateTerrainData() 
        {
            TerrainData data = new TerrainData();
            data.heightmapResolution = Settings.HeightmapResolution;
            data.alphamapResolution = Settings.AlphamapResolution;
            data.SetHeights(0, 0, Heightmap);
            ApplyTextures(data);
            data.size = new Vector3(Settings.Length, Settings.Height, Settings.Length);
            return data;
        }

        // Creates Terrain game object based on TerrainData and returns Terrain component
        private Terrain GenerateTerrain(TerrainData data) 
        {
            GameObject newTerrainGameObject = Terrain.CreateTerrainGameObject(data);
            newTerrainGameObject.transform.position = new Vector3(Position.X * Settings.Length, 0, Position.Z * Settings.Length);
            newTerrainGameObject.gameObject.name = GenerateTerrainName();
            newTerrainGameObject.transform.parent = TerrainRoot.transform; // Set chunk as child of TerrainGenerator transform
            return newTerrainGameObject.GetComponent<Terrain>();
        }
        
        // Creates a unique name for terrain gameObject
        private string GenerateTerrainName()
        {
            string posX = (Position.X * Settings.Length).ToString();
            string posY = "0";
            string posZ = (Position.Z * Settings.Length).ToString();
            return "Terrain " + posX + ", " + posY + ", " + posZ;
        }

        private void ApplyTextures(TerrainData terrainData)
        {
            var flatSplat = new SplatPrototype();
            var steepSplat = new SplatPrototype();

            flatSplat.texture = Settings.FlatTexture;
            steepSplat.texture = Settings.SteepTexture;

            terrainData.splatPrototypes = new SplatPrototype[]
            {
                flatSplat,
                steepSplat
            };

            terrainData.RefreshPrototypes();

            var splatMap = new float[terrainData.alphamapResolution, terrainData.alphamapResolution, 2];

            for (var zRes = 0; zRes < terrainData.alphamapHeight; zRes++)
            {
                for (var xRes = 0; xRes < terrainData.alphamapWidth; xRes++)
                {
                    var normalizedX = (float)xRes / (terrainData.alphamapWidth - 1);
                    var normalizedZ = (float)zRes / (terrainData.alphamapHeight - 1);

                    var steepness = terrainData.GetSteepness(normalizedX, normalizedZ);
                    var steepnessNormalized = Mathf.Clamp(steepness / 1.5f, 0, 1f);

                    splatMap[zRes, xRes, 0] = 1f - steepnessNormalized;
                    splatMap[zRes, xRes, 1] = steepnessNormalized;
                }
            }

            terrainData.SetAlphamaps(0, 0, splatMap);
        }

        #endregion

        #region Distinction

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as TerrainChunk;
            if (other == null)
                return false;

            return this.Position.Equals(other.Position);
        }

        #endregion

        #region Chunk removal

        public void Remove()
        {
            Heightmap = null;
            Settings = null;

            if (Neighborhood.XDown != null)
            {
                Neighborhood.XDown.RemoveFromNeighborhood(this);
                Neighborhood.XDown = null;
            }
            if (Neighborhood.XUp != null)
            {
                Neighborhood.XUp.RemoveFromNeighborhood(this);
                Neighborhood.XUp = null;
            }
            if (Neighborhood.ZDown != null)
            {
                Neighborhood.ZDown.RemoveFromNeighborhood(this);
                Neighborhood.ZDown = null;
            }
            if (Neighborhood.ZUp != null)
            {
                Neighborhood.ZUp.RemoveFromNeighborhood(this);
                Neighborhood.ZUp = null;
            }

            if (Terrain != null)
                GameObject.Destroy(Terrain.gameObject);
        }

        public void RemoveFromNeighborhood(TerrainChunk chunk)
        {
            if (Neighborhood.XDown == chunk)
                Neighborhood.XDown = null;
            if (Neighborhood.XUp == chunk)
                Neighborhood.XUp = null;
            if (Neighborhood.ZDown == chunk)
                Neighborhood.ZDown = null;
            if (Neighborhood.ZUp == chunk)
                Neighborhood.ZUp = null;
        }

        #endregion

        #region Neighborhood

        public void SetNeighbors(TerrainChunk chunk, TerrainNeighbor direction)
        {
            if (chunk != null)
            {
                switch (direction)
                {
                    case TerrainNeighbor.XUp:
                        Neighborhood.XUp = chunk;
                        break;

                    case TerrainNeighbor.XDown:
                        Neighborhood.XDown = chunk;
                        break;

                    case TerrainNeighbor.ZUp:
                        Neighborhood.ZUp = chunk;
                        break;

                    case TerrainNeighbor.ZDown:
                        Neighborhood.ZDown = chunk;
                        break;
                }
            }
        }

        public void UpdateNeighbors()
        {
            if (Terrain != null)
            {
                var xDown = Neighborhood.XDown == null ? null : Neighborhood.XDown.Terrain;
                var xUp = Neighborhood.XUp == null ? null : Neighborhood.XUp.Terrain;
                var zDown = Neighborhood.ZDown == null ? null : Neighborhood.ZDown.Terrain;
                var zUp = Neighborhood.ZUp == null ? null : Neighborhood.ZUp.Terrain;
                Terrain.SetNeighbors(xDown, zUp, xUp, zDown);
                Terrain.Flush();
            }
        }

        #endregion
    }
}