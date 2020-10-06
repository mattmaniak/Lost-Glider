﻿#undef DEBUG

using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    const int spritesNumberMin = 3;
    readonly int spritesNumber = 4;
    readonly string[] airStreamSuffixes = { "cold", "hot" };
    readonly Vector2 graveyardPosition = new Vector2(-100.0f, 0.0f);

    [SerializeField]
    GameObject airStreamPrefab;

    [SerializeField]
    GameObject groundChunkPrefab;

    GameObject airStreamsParent;
    GameObject groundChunksParent;

    bool initialGroundChunk;
    float cameraHalfWidthInWorld;
    float groundChunkWidth;
    float nextGroundChunkTransitionX;
    int currentGroundChunkIndex;
    int nextGroundChunkIndex;
    int previousGroundChunkIndex;
    List<GameObject> airStreamsPool = new List<GameObject>();
    List<GameObject> groundChunksPool = new List<GameObject>();

    float CameraLeftEdgeInWorldX
    {
        get => Camera.main.transform.position.x - cameraHalfWidthInWorld;
    }

    float GroundChunkHalfWidth
    {
        get => groundChunkWidth / 2.0f;
    }

    void Start()
    {
        airStreamsParent = new GameObject("AirStreamsPool");
        groundChunksParent = new GameObject("GroundChunksPool");

        cameraHalfWidthInWorld = Camera.main.ScreenToWorldPoint(new Vector3(
            Screen.width, 0.0f, 0.0f)).x;
        try
        {
            InitializeAirStreamPool();
            InitializeGroundChunksPool();
        }
        catch (System.Exception ex)
        {
#if DEBUG
            Debug.Log(ex);
#endif
            UnityQuit.Quit(1);
        }
    }

    void Update()
    {   
        GenerateInfiniteGround();
    }

    float CenterObjectVertically(in GameObject gameObject) =>
        gameObject.GetComponent<SpriteRenderer>().sprite.bounds.size.y / 2.0f;

    GameObject CreateEntityFromPrefab(GameObject prefab, string basename)
    {
        BoxCollider2D prefabCollider;
        Sprite prefabSprite;
        SpriteRenderer prefabRenderer;

        if ((prefabSprite = Resources.Load<Sprite>(basename)) == null)
        {
            string errMsg = GetType().Name
                            + " initialization aborted. Unable to load: "
                            + basename;
#if DEBUG
            Debug.Log(errMsg);
#endif
            throw new FileNotFoundException(errMsg);
        }
        prefab.transform.position = graveyardPosition;
    
        prefabCollider = prefab.GetComponent<BoxCollider2D>();
        prefabRenderer = prefab.GetComponent<SpriteRenderer>();

        prefabRenderer.sprite = prefabSprite;
        prefabCollider.size = prefabRenderer.sprite.bounds.size;
        prefabCollider.offset = prefabRenderer.sprite.bounds.center;

        return prefab;
    }

    void GenerateInfiniteGround()
    {
        if (CameraLeftEdgeInWorldX >= nextGroundChunkTransitionX)
        {
            previousGroundChunkIndex = currentGroundChunkIndex;
            if (!initialGroundChunk)
            {
                currentGroundChunkIndex = nextGroundChunkIndex;
            }
            else
            {
                initialGroundChunk = false;
            }

            do
            {
                nextGroundChunkIndex = Random.Range(0, spritesNumber);
            }
            while ((nextGroundChunkIndex == previousGroundChunkIndex)
                   || (nextGroundChunkIndex == currentGroundChunkIndex));

            for (int i = 0; i < spritesNumber; i++)
            {
                if (i == nextGroundChunkIndex)
                {
                    groundChunksPool[i].transform.position = new Vector2(
                        nextGroundChunkTransitionX + groundChunkWidth
                        + GroundChunkHalfWidth,
                        CenterObjectVertically(groundChunksPool[i]));
                }
                else if (i != currentGroundChunkIndex)
                {
                    groundChunksPool[i].transform.position = graveyardPosition;
                }
            }
            nextGroundChunkTransitionX += groundChunkWidth;
        }
    }

    void InitializeAirStreamPool()
    {
        foreach (string suffix in airStreamSuffixes)
        {
            airStreamsPool.Add(Instantiate(CreateEntityFromPrefab(
                airStreamPrefab, "Sprites/Level/air_stream_" + suffix)));
            
            airStreamsPool[airStreamsPool.Count - 1].transform.parent
                = airStreamsParent.transform;
        }
    }

    void InitializeGroundChunksPool()
    {
        initialGroundChunk = true;

        currentGroundChunkIndex = Random.Range(0, spritesNumber);
        previousGroundChunkIndex = -1;

        nextGroundChunkTransitionX = CameraLeftEdgeInWorldX;

        if (spritesNumber < spritesNumberMin)
        {
#if DEBUG
            Debug.Log(GetType().Name
                +" initialization aborted. At least 3 ground sprites needed.");
#endif
            UnityQuit.Quit(1);
        }
        for (int i = 0; i < spritesNumber; i++)
        {
            groundChunksPool.Add(Instantiate(CreateEntityFromPrefab(
                groundChunkPrefab, "Sprites/Level/ground_chunk_" + i)));

            groundChunksPool[i].transform.parent = groundChunksParent.transform;

            if (i == currentGroundChunkIndex)
            {
                var groundChunk = groundChunksPool[i];

                groundChunkWidth = groundChunk.GetComponent<SpriteRenderer>().
                                   sprite.bounds.size.x;

                groundChunk.transform.position = new Vector2(
                    GroundChunkHalfWidth - cameraHalfWidthInWorld,
                    CenterObjectVertically(groundChunk));
            }
        }
    }
}
