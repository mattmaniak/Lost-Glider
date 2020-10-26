﻿using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Level
{
    class Generator : MonoBehaviour
    {
        const int groundChunksMin = 3;
        readonly Vector2 graveyardPosition = new Vector2(-100.0f, 0.0f);

        [SerializeField]
        GameObject atmosphericPhenomenaPrefab;

        [SerializeField]
        GameObject groundChunkPrefab;

        GameObject atmosphericPhenomenaParent;
        GameObject groundChunksParent;
        int? previousAirStreamIndex;
        int? previousGroundChunkIndex;
        List<GameObject> atmosphericPhenomenaPool = new List<GameObject>();
        List<GameObject> groundChunksPool = new List<GameObject>();

        bool InitialAtmosphericPhenomenon { get; set; } 
        bool InitialGroundChunk { get; set; }
        float CameraHalfWidthInWorld { get; set; }

        float CameraLeftEdgeInWorldX
        {
            get => Camera.main.transform.position.x - CameraHalfWidthInWorld
                   + Camera.main.transform.localPosition.x;
        }

        float CameraWidthInWorld { get => CameraHalfWidthInWorld * 2.0f; }
        float GroundChunkHalfWidth { get => GroundChunkWidth / 2.0f; }
        float GroundChunkWidth { get; set; }
        float NextGroundChunkTransitionX { get; set; }
        int CurrentGroundChunkIndex { get; set; }
        int NextAirStreamIndex { get; set; }
        int NextGroundChunkIndex { get; set; }

        void Start()
        {
            atmosphericPhenomenaParent
                = new GameObject("AtmosphericPhenomenaPool");
            groundChunksParent = new GameObject("GroundChunksPool");

            CameraHalfWidthInWorld = Camera.main.ScreenToWorldPoint(new Vector3(
                Screen.width, 0.0f, 0.0f)).x;
            try
            {
                InitializeAtmosphericPhenomenaPool();
                InitializeGroundChunksPool();
            }
            catch (System.Exception ex)
            {
                if (DebugUtils.GlobalEnabler.activated)
                {
                    Debug.Log(ex);
                }
                Utils.UnityQuit.Quit(1);
            }
        }

        void Update()
        {   
            GenerateInfiniteGround();
            GenerateSoaringLiftsInfinitely();
        }

        float CenterObjectVertically(in GameObject go) =>
            go.GetComponent<SpriteRenderer>().sprite.bounds.size.y / 2.0f;

        GameObject CreateObjectFromPrefab(in GameObject sourcePrefab,
                                          string basename)
        {
            BoxCollider2D goBoxCollider;
            GameObject go;
            Sprite goSprite;
            SpriteRenderer goSpriteRenderer;

            go = Instantiate(sourcePrefab);

            if ((goSprite = Resources.Load<Sprite>(basename)) == null)
            {
                string errMsg = GetType().Name
                                + " initialization aborted. Unable to load: "
                                + basename;

                if (DebugUtils.GlobalEnabler.activated)
                {
                    Debug.Log(errMsg);
                }
                throw new FileNotFoundException(errMsg);
            }
            go.transform.position = graveyardPosition;
        
            goBoxCollider = go.GetComponent<BoxCollider2D>();
            goSpriteRenderer = go.GetComponent<SpriteRenderer>();

            goSpriteRenderer.sprite = goSprite;
            goBoxCollider.size = goSpriteRenderer.sprite.bounds.size;
            goBoxCollider.offset = goSpriteRenderer.sprite.bounds.center;

            return go;
        }

        void GenerateSoaringLiftsInfinitely()
        {
            const float maxOffCameraOffsetY = 1.0f;
            const float minOffScreenOffsetX = 1.0f;
            const float maxOffScreenOffsetX = 10.0f;

            GameObject atmosphericPhenomenon 
                = atmosphericPhenomenaPool[NextAirStreamIndex];
            Vector2 newPosition;

            if (InitialAtmosphericPhenomenon || (CameraLeftEdgeInWorldX
                >= (atmosphericPhenomenon.transform.position.x
                    + (atmosphericPhenomenon.GetComponent<SpriteRenderer>().
                       bounds.size.x / 2.0f))))
            {
                previousAirStreamIndex = NextAirStreamIndex;
                do
                {
                    NextAirStreamIndex
                        = Random.Range(0, atmosphericPhenomenaPool.Count);
                }
                while (NextAirStreamIndex == previousAirStreamIndex);

                newPosition.x = Random.Range(CameraLeftEdgeInWorldX
                    + CameraWidthInWorld + minOffScreenOffsetX,
                    CameraLeftEdgeInWorldX + CameraWidthInWorld
                    + maxOffScreenOffsetX
                    - Camera.main.transform.localPosition.x);

                newPosition.y = Random.Range(Camera.main.transform.position.y
                                             - maxOffCameraOffsetY,
                                             Camera.main.transform.position.y
                                             + maxOffCameraOffsetY);

                atmosphericPhenomenaPool[NextAirStreamIndex].transform.position
                    = newPosition;
                InitialAtmosphericPhenomenon = false;
            }
        }

        void GenerateInfiniteGround()
        {
            if (CameraLeftEdgeInWorldX >= NextGroundChunkTransitionX)
            {
                previousGroundChunkIndex = CurrentGroundChunkIndex;
                if (!InitialGroundChunk)
                {
                    CurrentGroundChunkIndex = NextGroundChunkIndex;
                }
                else
                {
                    InitialGroundChunk = false;
                }

                do
                {
                    NextGroundChunkIndex
                        = Random.Range(0, groundChunksPool.Count);
                }
                while ((NextGroundChunkIndex == previousGroundChunkIndex)
                       || (NextGroundChunkIndex == CurrentGroundChunkIndex));

                for (int i = 0; i < groundChunksPool.Count; i++)
                {
                    if (i == NextGroundChunkIndex)
                    {
                        groundChunksPool[i].transform.position = new Vector2(
                            NextGroundChunkTransitionX + GroundChunkWidth
                            + GroundChunkHalfWidth
                            - Camera.main.transform.localPosition.x,
                            CenterObjectVertically(groundChunksPool[i]));
                    }
                    else if (i != CurrentGroundChunkIndex)
                    {
                        groundChunksPool[i].transform.position
                            = graveyardPosition;
                    }
                }
                NextGroundChunkTransitionX += GroundChunkWidth;
            }
        }

        void InitializeAtmosphericPhenomenaPool()
        {
            const string spritesPath = @"Sprites/Level/AtmosphericPhenomena/";

            string[] spritesNames = Directory.GetFiles(
                Path.Combine(@"Assets/Resources/", spritesPath), "*.psd");
            int initialStreamIndex = Random.Range(0, spritesNames.Length);

            InitialAtmosphericPhenomenon = true;

            foreach (var name in spritesNames)
            {
                var spriteName = Path.GetFileNameWithoutExtension(name);

                try
                {
                    atmosphericPhenomenaPool.Add(CreateObjectFromPrefab(
                        atmosphericPhenomenaPrefab,
                        Path.Combine(spritesPath, spriteName)));
                }
                catch (FileNotFoundException ex)
                {
                    if (DebugUtils.GlobalEnabler.activated)
                    {
                        Debug.Log(ex);
                    }
                    Utils.UnityQuit.Quit(1);
                }            
                atmosphericPhenomenaPool[atmosphericPhenomenaPool.Count - 1].
                    transform.parent = atmosphericPhenomenaParent.transform;

                var atmosphericPhenomenon = atmosphericPhenomenaPool
                    [atmosphericPhenomenaPool.Count - 1].
                    GetComponent<AtmosphericPhenomenon>();
            }
        }

        void InitializeGroundChunksPool()
        {
            const string spriteNamePrefix = @"ground_chunk_";
            const string spritesPath = @"Sprites/Level/GroundChunks/";

            string[] spritesNames = Directory.GetFiles(
                Path.Combine(@"Assets/Resources/", spritesPath), "*.psd");

            CurrentGroundChunkIndex = Random.Range(0, spritesNames.Length);
            NextGroundChunkTransitionX = CameraLeftEdgeInWorldX;
            InitialGroundChunk = true;

            if (spritesNames.Length < groundChunksMin)
            {
                if (DebugUtils.GlobalEnabler.activated)
                {
                    Debug.Log(GetType().Name + " initialization aborted. "
                              + $"At least {groundChunksMin} grounds needed.");
                }
                Utils.UnityQuit.Quit(1);
            }
            for (int i = 0; i < spritesNames.Length; i++)
            {
                try
                {
                    groundChunksPool.Add(CreateObjectFromPrefab(
                        groundChunkPrefab,
                        Path.Combine(spritesPath, spriteNamePrefix) + i));
                }
                catch (FileNotFoundException ex)
                {
                    if (DebugUtils.GlobalEnabler.activated)
                    {
                        Debug.Log(ex);
                    }
                    Utils.UnityQuit.Quit(1);
                }
                groundChunksPool[i].transform.parent
                    = groundChunksParent.transform;

                if (i == CurrentGroundChunkIndex)
                {
                    var groundChunk = groundChunksPool[i];

                    GroundChunkWidth = groundChunk.
                        GetComponent<SpriteRenderer>().sprite.bounds.size.x;

                    groundChunk.transform.position = new Vector2(
                        GroundChunkHalfWidth - CameraHalfWidthInWorld
                        + Camera.main.transform.localPosition.x,
                        CenterObjectVertically(groundChunk));
                }
            }
        }
    }
}
