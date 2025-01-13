using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using Gley;

namespace RealEstateTycoon
{
	public class MenuManager : MonoBehaviour
	{
		/// <summary>
		/// Main Menu Buttons Controller.
		/// </summary>
		/// 

		public Text languageButtonText;

		public Text playerBestTimeText;
		private int bestTime;

		public Text playerHighestMoneyText;
		private int highestMoney;

		public AudioClip menuTap;
		private bool canTap;

		void Awake()
		{
			Time.timeScale = 1.0f;
			canTap = true; //player can tap on buttons

			//if this is the first run, init bestTime variable (set it too high).
			//player has to break this record by decreasing it in time-trial mode.
			if (!PlayerPrefs.HasKey("bestTime"))
				PlayerPrefs.SetInt("bestTime", 3599); //default value = 59':59"

			bestTime = PlayerPrefs.GetInt("bestTime");
			int seconds = Mathf.CeilToInt(bestTime) % 60;
			int minutes = Mathf.CeilToInt(bestTime) / 60;
			playerBestTimeText.text = String.Format("{0:00}' : {1:00}'' ", minutes, seconds);

			highestMoney = PlayerPrefs.GetInt("highestMoney");
			playerHighestMoneyText.text = "$" + highestMoney;
		}

		void Start()
		{
			//prevent screenDim in handheld devices
			Screen.sleepTimeout = SleepTimeout.NeverSleep;

			languageButtonText.text = Gley.Localization.API.GetCurrentLanguage().ToString();
		}


		/// <summary>
		/// play audio clip
		/// </summary>
		/// <param name="_sfx"></param>
		void PlaySfx(AudioClip _sfx)
		{
			GetComponent<AudioSource>().clip = _sfx;
			if (!GetComponent<AudioSource>().isPlaying)
				GetComponent<AudioSource>().Play();
		}


		public void ClickOnCareerButton()
		{
			if (!canTap)
				return;
			canTap = false;

			PlaySfx(menuTap);
			PlayerPrefs.SetString("gameMode", "CAREER");
			StartCoroutine(LoadSceneCo("CareerMap"));
		}

		public void ClickOnTimeButton()
		{
			if (!canTap)
				return;
			canTap = false;

			PlaySfx(menuTap);
			PlayerPrefs.SetString("gameMode", "TIMETRIAL");
			StartCoroutine(LoadSceneCo("Game"));
		}

		public void ClickOnEndlessButton()
		{
			if (!canTap)
				return;
			canTap = false;

			PlaySfx(menuTap);
			PlayerPrefs.SetString("gameMode", "ENDLESS");
			StartCoroutine(LoadSceneCo("Game"));
		}

		public void ClickOnLanguageButton()
		{
			Gley.Localization.API.NextLanguage();
			languageButtonText.text = Gley.Localization.API.GetCurrentLanguage().ToString();
		}


		public IEnumerator LoadSceneCo(string sceneToLoad = "Game")
		{
			yield return new WaitForSeconds(0.5f);
			SceneManager.LoadScene(sceneToLoad);
		}

	}
}