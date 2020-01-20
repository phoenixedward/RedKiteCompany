﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RedKite
{
    public class GameSpriteManager : MonoBehaviour
    {
        private static GameSpriteManager instance;

        public static GameSpriteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GameSpriteManager>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject
                        {
                            name = typeof(GameSpriteManager).Name
                        };
                        instance = obj.AddComponent<GameSpriteManager>();
                    }
                }
                return instance;
            }
        }

        public List<GameSprite> Sprites { get; private set; } = new List<GameSprite>();
        public List<Hero> Heroes { get; private set; } = new List<Hero>();
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            Sprites = FindObjectsOfType<GameSprite>().ToList();

            foreach (GameSprite sprite in Sprites)
            {
                if (sprite is Hero)
                    Heroes.Add((Hero)sprite);
                else if(sprite is Enemy)
                    Enemies.Add((Enemy)sprite);
            }

        }


    }
}