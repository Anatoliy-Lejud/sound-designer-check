using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Assets.UBindr.Examples.AllInOne
{
    // Demo View Model
    public class Settings : MonoBehaviour
    {
        public Settings()
        {
            Players = new List<Player>
            {
                new Player{Name="Mr Splendid", Score = 75, Settings = this},
                new Player{Name="Xerxes", Score = 15, Settings = this},
                new Player{Name="Madam GodMode", Score = 100, Settings = this},
            };
            selectedPlayer = Players[0];
        }

        public string version = "v 2.1";
        public string serverName = "my server name";

        [Range(0, 30)]
        public float volume;

        [Range(10, 30)]
        public int intSliderValue = 28;

        public float float1;
        public int int1;

        public string header { get { return "This is my header " + Time.time.ToString("0.0"); } }
        public int PlayerCount { get { return Players.Count; } }

        public float Val1;
        public float Val2;
        public float Val3;
        public float Val4;

        public bool Toggle1;
        public bool Toggle2;

        public float Stars { get { return Blip(0.33f) * 4; } }

        public int selectedPlayerId
        {
            get
            {
                return Players.IndexOf(selectedPlayer);
            }
            set
            {
                selectedPlayer = Players[value];
            }
        }

        public List<Player> Players { get; set; }

        public Color RandomColor { get; set; }

        public void Update()
        {
            Val1 = Blip(0);
            Val2 = Blip(0.25f);
            Val3 = Blip(0.5f);
            Val4 = Blip(0.75f);
            if (!Players.Contains(selectedPlayer))
            {
                selectedPlayer = null;
            }

            if (Time.frameCount % 50 == 0)
            {
                RandomColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.25f, 1f);
            }
        }

        public Sprite[] sprites;
        public Sprite selectedSprite { get { return sprites[selectedDropdownOption]; } }
        public Player selectedPlayer;

        public Sprite GetSprite(int spriteId)
        {
            return sprites[spriteId];
        }

        public int selectedDropdownOption = 2;


        public List<Dropdown.OptionData> DropdownOptions
        {
            get { return Options.Select(x => new Dropdown.OptionData { text = x }).ToList(); }
        }

        public int selectedOption = 1;
        public List<string> Options = new List<string> { "First Option", "Second Option", "Third Option" };


        public void DeleteLastPlayer()
        {
            if (Players.Any())
            {
                Players.Remove(Players.OrderByDescending(x => x.Score).Last());
            }
        }

        public void AddRandomPlayer()
        {
            selectedPlayer = new Player { Name = Guid.NewGuid().ToString(), Score = Random.value * 100, Settings = this };
            Players.Add(selectedPlayer);
        }

        public void ChangeVolume(float delta)
        {
            volume += delta;
        }

        public float Blip(float timeOffset)
        {
            return Mathf.Sin((Time.time + timeOffset) * volume) * 0.5f + 0.5f;
        }

        public void Log(string value)
        {
            Debug.Log(value);
        }

        public class Player
        {
            public string Name;
            public float Score;
            public Settings Settings;
            //public override string ToString() { return Name; }

            public void SelectMe()
            {
                Settings.selectedPlayer = this;
            }

            public void DeleteMe()
            {
                Settings.Players.Remove(this);
            }

            public void ChangeScore(float delta)
            {
                Score += delta;
                SelectMe();
            }
        }
    }
}
