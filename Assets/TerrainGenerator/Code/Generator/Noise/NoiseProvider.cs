using UnityEngine;

using LibNoise.Generator;

namespace TerrainGenerator
{
    public class NoiseProvider : INoiseProvider
    {
		private float height;
		private float damper;

        private Perlin PerlinNoiseGenerator;

        public NoiseProvider(float perlinHeight,float perlinDamper)
        {
			height = perlinHeight;
			damper = perlinDamper;
            PerlinNoiseGenerator = new Perlin();
        }

        public float GetValue(float x, float z)
        {
            return (float)(PerlinNoiseGenerator.GetValue(x, 0, z) / height) + damper;
        }

    }
}