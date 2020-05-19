using System.Collections;
using TerrainGenerator;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private Vector2i previousPlayerChunkPosition;
    public Transform playerTransform;
    public TerrainChunkGenerator Generator;
	
    public void Start()
    {
        playerTransform.gameObject.SetActive(false);
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        bool terrainGenerated = false;
        Generator.UpdateTerrain(playerTransform.position);
        do{
            terrainGenerated = Generator.IsTerrainAvailable(playerTransform.position);
            yield return null;
        } while (!terrainGenerated);
        ActivatePlayer();
    }

    private void ActivatePlayer() {
        previousPlayerChunkPosition = Generator.GetChunkPosition(playerTransform.position);
        Vector3 newPosition = new Vector3(
            playerTransform.position.x, 
            Generator.GetTerrainHeight(playerTransform.position) + 0.5f, 
            playerTransform.position.z
        );
        playerTransform.position = newPosition;
        playerTransform.gameObject.SetActive(true);
    }

    private void Update()
    {
        var playerChunkPosition = Generator.GetChunkPosition(playerTransform.position);
        if (!playerChunkPosition.Equals(previousPlayerChunkPosition)) {
            Generator.UpdateTerrain(playerTransform.position);
            previousPlayerChunkPosition = playerChunkPosition;
        }
    }

}