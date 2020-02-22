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
            (float xr, float yr, float zr) = rotateXZBeforeY(x, 0, z);
            return (float)(PerlinNoiseGenerator.GetValue(xr, yr, zr) / height) + damper;
        }

        // Old Perlin noise can be visibly grid-aligned. Simplex-type noise is usually better about that.
        // However, in the absence of Simplex, 3D Perlin can be rotated to hide the grid in X/Z planes.
        private (float xr, float yr, float zr) rotateXZBeforeY(float x, float y, float z)
        {
            float xz = x + z;
            float yy = y * 0.577350269189626f;
            float s2 = xz * -0.211324865405187f + yy;
            float xr = x + s2;
            float zr = z + s2;
            float yr = xz * -0.577350269189626f + yy;
            return (xr, yr, zr);
        }

    }
}