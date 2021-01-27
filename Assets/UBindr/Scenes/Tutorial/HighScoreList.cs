using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.UBindr.Examples.Tutorial
{
    public class HighScoreList : MonoBehaviour
    {
        public HighScoreList()
        {
            Singleton = this;

            Players = new List<Player>
            {
                new Player{Name="Mr Splendid", Score = 75},
                new Player{Name="Xerxes", Score = 15},
                new Player{Name="Madam GodMode", Score = 100},
            };
            selectedPlayer = Players[0];
        }

        public static HighScoreList Singleton { get; set; }
        public List<Player> Players { get; set; }
        public Player selectedPlayer;

        public void AddPlayer()
        {
            selectedPlayer = new Player { Name = Guid.NewGuid().ToString(), Score = Random.value * 100 };
            Players.Add(selectedPlayer);
        }

        public class Player
        {
            public string Name;
            public float Score;

            public void SelectMe()
            {
                HighScoreList.Singleton.selectedPlayer = this;
            }

            public void DeleteMe()
            {
                HighScoreList.Singleton.Players.Remove(this);
            }

            public void ChangeScore(float delta)
            {
                Score += delta;
                SelectMe();
            }
        }
    }
}