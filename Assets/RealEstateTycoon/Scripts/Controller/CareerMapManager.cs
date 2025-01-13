using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

namespace RealEstateTycoon
{
	public class CareerMapManager : MonoBehaviour
	{
		/// <summary>
		/// CareerMapManager will load the game scene with parameters set by you
		/// for the selected level. It will saves those values inside playerPrefs and
		/// tehy will be fetched and applied in the game scene.
		/// </summary>

		public static int userLevelAdvance;
		private int totalLevels;
		private GameObject[] levels;

		public AudioClip menuTap;
		private bool canTap;

		void Awake()
		{
			Time.timeScale = 1.0f;
			canTap = true; //player can tap on buttons

			if (PlayerPrefs.HasKey("userLevelAdvance"))
				userLevelAdvance = PlayerPrefs.GetInt("userLevelAdvance");
			else
				userLevelAdvance = 0; //default. only level 1 in open.


			//get total levels
			levels = GameObject.FindGameObjectsWithTag("levelSelectionPin");
			totalLevels = levels.Length;

			//Lock all levels
			for (int i = 0; i < totalLevels; i++)
			{
				//levels[i].GetComponent<ItemMover>().enabled = false;
				//levels[i].GetComponent<BoxCollider>().enabled = false;
				levels[i].transform.parent.GetComponent<Animator>().enabled = false;
				levels[i].GetComponent<Button>().enabled = false;

				print(levels[i].name);
			}

			//unlock levels based on user level
			for (int j = 0; j < totalLevels; j++)
			{
				if (userLevelAdvance >= levels[j].GetComponent<CareerLevelSetup>().levelID - 1)
				{
					//levels[j].GetComponent<ItemMover>().enabled = true;
					//levels[j].GetComponent<BoxCollider>().enabled = true;
					levels[j].transform.parent.GetComponent<Animator>().enabled = true;
					levels[j].GetComponent<Button>().enabled = true;
				}
			}
		}

		void Start()
		{
			//prevent screenDim in handheld devices
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
		}



		///***********************************************************************
		/// play audio clip
		///***********************************************************************
		void PlaySfx(AudioClip _sfx)
		{
			GetComponent<AudioSource>().clip = _sfx;
			if (!GetComponent<AudioSource>().isPlaying)
				GetComponent<AudioSource>().Play();
		}


		public void GoToMenu()
		{
			PlaySfx(menuTap);
			StartCoroutine(LoadMenuCo());
		}

		public IEnumerator LoadMenuCo()
		{
			yield return new WaitForSeconds(0.5f);
			SceneManager.LoadScene("Menu");
		}


		public void LoadMission(CareerLevelSetup missionSettings)
		{
			if (!canTap)
				return;
			canTap = false;

			PlaySfx(menuTap);

			//save the game mode
			PlayerPrefs.SetString("gameMode", "CAREER");
			PlayerPrefs.SetInt("careerLevelID", missionSettings.levelID);

			//save mission variables
			PlayerPrefs.SetInt("careerGoalBallance", missionSettings.careerGoalBallance);
			PlayerPrefs.SetInt("careerAvailableTime", missionSettings.careerAvailableTime);
			PlayerPrefs.SetInt("careerStartingBallance", missionSettings.careerStartingBallance);

			//save available tiers for this level
			//we have 4 tiers, so...
			for (int i = 0; i < 4; i++)
			{
				PlayerPrefs.SetInt("careerTierAvailable_" + i.ToString(), Convert.ToInt32(missionSettings.availableTiers[i]));
			}

			StartCoroutine(LoadGameCo());
		}


		public IEnumerator LoadGameCo()
		{
			yield return new WaitForSeconds(0.5f);
			SceneManager.LoadScene("Game");
		}

	}
}