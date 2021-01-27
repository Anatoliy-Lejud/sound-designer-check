using System.Linq;
using UnityEngine;

namespace Assets.UBindr.Examples.Tutorial
{
    public class TutorialSettings : MonoBehaviour
    {
        public bool musicEnabled = true;
        [Range(0, 100)]
        public float musicVolume = 70;

        public bool soundEffectsEnabled = true;
        [Range(0, 100)]
        public float soundEffectsVolume = 70;

        public string message = "Way to go!";

        public int difficulty = 1;
        public string[] difficulties = new[] { "Easy", "Hard", "Suuper Hard", "Nightmare" };

        public void SoundFest()
        {
            message = "FIESTA!";
            musicVolume += 1;
            soundEffectsVolume -= 1;
            var diff = difficulties.ToList();
            diff.Add("Festive!");
            difficulties = diff.ToArray();
        }

        public void OpenInBrowser(string url)
        {            
            Application.OpenURL(url);
        }
    }
}

////public string shoutout = "Way to go!";
////public int selectedItem;
//
//[Range(0, 100)]

