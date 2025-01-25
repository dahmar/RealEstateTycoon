using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using Gley.Localization;

namespace RealEstateTycoon
{
	public class globalGameController : MonoBehaviour
	{
		/// <summary>
		/// Main controller for all global game events, like counters, balance, etc
		// It is also responsible for the main game loop, like:
		// -Spawning new buildings
		// -Detecting different states of the game like win or lose
		//
		// We have three types of level:
		// 1. Time-Management. Levels that have a certain time and player should achieve a goal:
		//	(certain amount of money)
		//	(purchasing several buildings)
		//	(purchasing a specific property)
		//	etc..
		// in that time limit. if the goal is not reached within the time, the game is lost.
		// -
		// 2. Endless. In this mode, time is not important. The player is free to play at
		// ease and can buy and sell properties to beat his/her highscore or meet the provided goal.
		// -
		// 3. Career. This is very similar to the time-trial mode, with the difference that we also
		// have control over the available tiers and can be used to make the final game more strategic 
		// by integrating a achievement system.
		/// </summary>

		public static globalGameController instance;

		// Mission Variables (for Endless mode) //
		public int endlessGoalBallance = 25000;
		public static int staticEndlessGoalBallance; //To use endlessGoalBallance in other classes 
													 //without needing the gameObject, while preserving the public type
													 //of the original variable "endlessGoalBallance"
													 //******************//
													 // Mission Variables (for TimeTrial & Career mode) //
		public int availableTime = 300;             //Seconds
		public int timetrialGoalBalance = 10000;    //Dollars
													//******************//
													// Common variables
		public int startingBallance = 100;          //available ballance at the start of the game
		public int newBuildingCreationDelay = 3;    //delay between creation of each building
		public bool[] availableTiers;               //4 tiers, all available (set to true)
													//******************//

		// Static Variables //
		//******************//
		public static string gameMode;
		public static int userCurrentBalance;
		public static int gameTime;
		public static int requiredBalance;
		public static bool gameIsFinished;
		//*******************
		private string remainingTime;
		private int seconds; private int minutes;
		//******************//

		// Public Variables //
		// References to gameObjects
		//******************//
		public Text modeIndicatorText;
		public GameObject spawnableProperty;
		public Text userBalanceText;
		public Text missionText;
		public Text timeText;
		public GameObject endGamePlane;
		public Image endGameStatus;
		public Text endGameText;
		public Sprite[] endGameTextures;

		//game controller spawn new building at the position of these dummy helpers
		public Transform[] dummyPositionsForTier_1;
		public Transform[] dummyPositionsForTier_2;
		public Transform[] dummyPositionsForTier_3;
		public Transform[] dummyPositionsForTier_4;

		//flag for dummy helpers to indicate if they are free or full
		public bool[] dummyPositionInUseTier_1;
		public bool[] dummyPositionInUseTier_2;
		public bool[] dummyPositionInUseTier_3;
		public bool[] dummyPositionInUseTier_4;

		//Audio clips
		public AudioClip houseCreationSfx;
		public AudioClip winSfx;
		public AudioClip loseSfx;
		public AudioClip timeUpSfx;

		//******************//

		// Private Variables //
		//******************//
		private bool propertyCreationFlag = true;
		private GameObject newProperty;
		//******************//

		void Awake()
		{
			instance = this;

			//First of all, check the game mode.
			if (PlayerPrefs.HasKey("gameMode"))
				gameMode = PlayerPrefs.GetString("gameMode");
			else
				gameMode = "ENDLESS"; //default game mode

			switch (gameMode)
			{
				case "ENDLESS":
					requiredBalance = endlessGoalBallance;
					gameTime = 0;
					break;
				case "TIMETRIAL":
					requiredBalance = timetrialGoalBalance;
					gameTime = availableTime;
					break;
				case "CAREER":
					requiredBalance = PlayerPrefs.GetInt("careerGoalBallance");
					availableTime = PlayerPrefs.GetInt("careerAvailableTime");
					startingBallance = PlayerPrefs.GetInt("careerStartingBallance");

					for (int i = 0; i < 4; i++)
					{
						availableTiers[i] = (PlayerPrefs.GetInt("careerTierAvailable_" + i.ToString()) == 1) ? true : false;
					}
					break;
			}

			staticEndlessGoalBallance = endlessGoalBallance; //we pass the static variable to other classes
			endGamePlane.SetActive(false);

		}

		void Start()
		{
			gameIsFinished = false;
			userCurrentBalance = startingBallance; //Starting Ballance in $
			missionText.text = "$" + requiredBalance.ToString();

			//TODO game mode translation
			string modeText = Gley.Localization.API.GetText(Gley.Localization.WordIDs.mode) + " ";

			if(gameMode == "ENDLESS") modeText += Gley.Localization.API.GetText(Gley.Localization.WordIDs.endless_mode_text);
			else if( gameMode == "TIMETRIAL") modeText += Gley.Localization.API.GetText(Gley.Localization.WordIDs.timetrial_mode_text);
			else modeText += Gley.Localization.API.GetText(Gley.Localization.WordIDs.career_mode_text);

			modeIndicatorText.text = modeText;//"Mode: " + gameMode;
		}

		void Update()
		{
			updateUserBallance();
			updateGameTime();
			createProperty();
			StartCoroutine(checkGameWinState());
		}

		/// <summary>
		/// Game clock manager
		// for the endless game, time counts forward, and fir the career and time-trial modes,
		// it runs backward.
		/// </summary>
		void updateGameTime()
		{
			if (gameIsFinished)
				return;

			if (gameMode == "ENDLESS")
			{
				gameTime = (int)Time.timeSinceLevelLoad;
				seconds = Mathf.CeilToInt(Time.timeSinceLevelLoad) % 60;
				minutes = Mathf.CeilToInt(Time.timeSinceLevelLoad) / 60;
				remainingTime = string.Format("{0:00} : {1:00}", minutes, seconds);
				timeText.text = remainingTime.ToString();
			}
			else if (gameMode == "TIMETRIAL" || gameMode == "CAREER")
			{
				gameTime = (int)(availableTime - Time.timeSinceLevelLoad);
				seconds = Mathf.CeilToInt(availableTime - Time.timeSinceLevelLoad) % 60;
				minutes = Mathf.CeilToInt(availableTime - Time.timeSinceLevelLoad) / 60;
				remainingTime = string.Format("{0:00} : {1:00}", minutes, seconds);
				timeText.text = remainingTime.ToString();
			}
		}

		/// <summary>
		/// Game Win/Lose State
		/// </summary>
		/// <returns></returns>
		IEnumerator checkGameWinState()
		{
			if (gameIsFinished)
				yield break;

			if (gameMode == "TIMETRIAL" && gameTime <= 0 && userCurrentBalance < requiredBalance)
			{
				print("Time is up! You have failed :(");    //debug the result
				gameIsFinished = true;                      //announce the new status to other classes
				endGamePlane.SetActive(true);               //show the endGame plane
				endGameStatus.sprite = endGameTextures[1];  //show the correct texture for result
				endGameText.text = Gley.Localization.API.GetText(WordIDs.you_have_lost);
				playNormalSfx(timeUpSfx);
				yield return new WaitForSeconds(2.0f);
				playNormalSfx(loseSfx);

			}
			else if (gameMode == "TIMETRIAL" && gameTime > 0 && userCurrentBalance >= requiredBalance)
			{
				print("Wow, You beat the level! :)");
				gameIsFinished = true;
				endGamePlane.SetActive(true);
				endGameStatus.sprite = endGameTextures[0];
				endGameText.text = Gley.Localization.API.GetText(WordIDs.you_win);
				playNormalSfx(winSfx);

				//save the best time in TimeTrial mode (saves the passed time)
				int lastBestTime = PlayerPrefs.GetInt("bestTime");
				if (Time.timeSinceLevelLoad < lastBestTime)
					PlayerPrefs.SetInt("bestTime", (int)Time.timeSinceLevelLoad);
			}
			else if (gameMode == "CAREER" && gameTime > 0 && userCurrentBalance >= requiredBalance)
			{
				//save career progress
				saveCareerProgress();
				print("Wow, You beat the level! :)");
				gameIsFinished = true;
				endGamePlane.SetActive(true);
				endGameStatus.sprite = endGameTextures[0];
				endGameText.text = Gley.Localization.API.GetText(WordIDs.you_win);
				playNormalSfx(winSfx);
			}
			else if (gameMode == "CAREER" && gameTime <= 0 && userCurrentBalance < requiredBalance)
			{
				print("Time is up! You have failed :(");
				gameIsFinished = true;
				endGamePlane.SetActive(true);
				endGameStatus.sprite = endGameTextures[1];
				endGameText.text = Gley.Localization.API.GetText(WordIDs.you_have_lost);
				playNormalSfx(timeUpSfx);
				yield return new WaitForSeconds(2.0f);
				playNormalSfx(loseSfx);
			}
			else if (gameMode == "ENDLESS" && userCurrentBalance >= requiredBalance)
			{
				print("Wow, You beat the goal in endless mode. You can continue to increase your highscore! :) Your score will be saved when you exit the game.");
				playNormalSfx(winSfx);
				//gameIsFinished = true; 
				//we can still play in endless mode. 
				//there is no end here unless user stops the game and choose exit.
			}
		}

		/// <summary>
		/// Save user progress in career mode.
		/// </summary>
		void saveCareerProgress()
		{
			int currentLevelID = PlayerPrefs.GetInt("careerLevelID");
			int userLevelAdvance = PlayerPrefs.GetInt("userLevelAdvance");

			//if this is the first time we are beating this level...
			if (userLevelAdvance < currentLevelID)
			{
				userLevelAdvance++;
				PlayerPrefs.SetInt("userLevelAdvance", userLevelAdvance);
			}
		}

		/// <summary>
		/// This function clones new buildings based on the chances available for each tier
		/// </summary>
		void createProperty()
		{
			if (gameIsFinished)
				return;

			//if we are allowed to clone a new building...
			if (propertyCreationFlag)
			{
				propertyCreationFlag = false;
				StartCoroutine(reactivePropertyCreationFlag()); //wait a few seconds, then create another building

				float tierChance = Random.value;    //used to create different kind of buildings
													//print("tierChance: " + tierChance);
													//temp cheat
													//tierChance = 0.2f;

				Transform creationPosition; //will be fetched from available dummy helpers
				int creationPositionIndex;  //random index from the available dummy helpers length

				if (tierChance >= 0 && tierChance < 0.25f && availableTiers[0])
				{
					creationPositionIndex = Random.Range(0, dummyPositionsForTier_1.Length);
					//if this position is not full...
					if (!dummyPositionInUseTier_1[creationPositionIndex])
					{
						creationPosition = dummyPositionsForTier_1[creationPositionIndex];  //fetch the position
						dummyPositionInUseTier_1[creationPositionIndex] = true; //mark this position as full
						newProperty = Instantiate(spawnableProperty, creationPosition.position, Quaternion.Euler(0, 0, 0)) as GameObject; //clone the main house controller
						newProperty.GetComponent<HouseController>().positionIndex = creationPositionIndex;  //set the position ID of this building it the relative tier array
						newProperty.GetComponent<HouseController>().houseTier = HouseController.tiers.cheap; //building's tier
						newProperty.GetComponent<HouseController>().priceIncreaseDuration = Random.Range(6, 15);    //available time for this building's price to go up
						newProperty.GetComponent<HouseController>().priceDecreaseDuration = Random.Range(5, 10);    //available time for this building's price to go down
						newProperty.GetComponent<HouseController>().allowedCycles = Random.Range(2, 4); //how many times this building can cycle it's price 
						playSfx(houseCreationSfx);
					}
				}
				else if (tierChance >= 0.25f && tierChance < 0.5f && availableTiers[1])
				{
					creationPositionIndex = Random.Range(0, dummyPositionsForTier_2.Length);
					if (!dummyPositionInUseTier_2[creationPositionIndex])
					{

						creationPosition = dummyPositionsForTier_2[creationPositionIndex];
						dummyPositionInUseTier_2[creationPositionIndex] = true;
						newProperty = Instantiate(spawnableProperty, creationPosition.position, Quaternion.Euler(0, 0, 0)) as GameObject;
						newProperty.GetComponent<HouseController>().positionIndex = creationPositionIndex;
						newProperty.GetComponent<HouseController>().houseTier = HouseController.tiers.medium;
						newProperty.GetComponent<HouseController>().priceIncreaseDuration = Random.Range(6, 15);
						newProperty.GetComponent<HouseController>().priceDecreaseDuration = Random.Range(5, 10);
						newProperty.GetComponent<HouseController>().allowedCycles = Random.Range(2, 4);
						playSfx(houseCreationSfx);
					}
				}
				else if (tierChance >= 0.5f && tierChance < 0.75f && availableTiers[2])
				{
					creationPositionIndex = Random.Range(0, dummyPositionsForTier_3.Length);
					if (!dummyPositionInUseTier_3[creationPositionIndex])
					{
						creationPosition = dummyPositionsForTier_3[creationPositionIndex];
						dummyPositionInUseTier_3[creationPositionIndex] = true;
						newProperty = Instantiate(spawnableProperty, creationPosition.position, Quaternion.Euler(0, 0, 0)) as GameObject;
						newProperty.GetComponent<HouseController>().positionIndex = creationPositionIndex;
						newProperty.GetComponent<HouseController>().houseTier = HouseController.tiers.expensive;
						newProperty.GetComponent<HouseController>().priceIncreaseDuration = Random.Range(6, 15);
						newProperty.GetComponent<HouseController>().priceDecreaseDuration = Random.Range(6, 10);
						newProperty.GetComponent<HouseController>().allowedCycles = Random.Range(1, 3);
						playSfx(houseCreationSfx);
					}
				}
				else if (tierChance >= 0.75f && tierChance < 1 && availableTiers[3] && Random.value > 0.6f)
				{
					creationPositionIndex = Random.Range(0, dummyPositionsForTier_4.Length);
					if (!dummyPositionInUseTier_4[creationPositionIndex])
					{
						creationPosition = dummyPositionsForTier_4[creationPositionIndex];
						dummyPositionInUseTier_4[creationPositionIndex] = true;
						newProperty = Instantiate(spawnableProperty, creationPosition.position, Quaternion.Euler(0, 0, 0)) as GameObject;
						newProperty.GetComponent<HouseController>().positionIndex = creationPositionIndex;
						newProperty.GetComponent<HouseController>().houseTier = HouseController.tiers.skyscrapper;
						newProperty.GetComponent<HouseController>().priceIncreaseDuration = Random.Range(6, 15);
						newProperty.GetComponent<HouseController>().priceDecreaseDuration = Random.Range(4, 8);
						newProperty.GetComponent<HouseController>().allowedCycles = Random.Range(1, 2);
						playSfx(houseCreationSfx);
					}
				}
			}
		}

		/// <summary>
		/// enable cloning of more buildings
		/// </summary>
		/// <returns></returns>
		IEnumerator reactivePropertyCreationFlag()
		{
			yield return new WaitForSeconds(newBuildingCreationDelay);
			propertyCreationFlag = true;
		}

		/// <summary>
		/// Awlays show user current ballance on the GUI
		/// </summary>
		void updateUserBallance()
		{
			userBalanceText.text = "$" + userCurrentBalance;
		}


		/// <summary>
		/// play oneshot audio clip
		/// </summary>
		/// <param name="_sfx"></param>
		void playSfx(AudioClip _sfx)
		{
			GetComponent<AudioSource>().clip = _sfx;
			GetComponent<AudioSource>().PlayOneShot(_sfx, 1.0f);
		}

		/// <summary>
		/// play normal audio clip
		/// </summary>
		/// <param name="_sfx"></param>
		void playNormalSfx(AudioClip _sfx)
		{
			GetComponent<AudioSource>().clip = _sfx;
			if (!GetComponent<AudioSource>().isPlaying)
				GetComponent<AudioSource>().Play();
		}

		public void AnimatePlayerBalanceUI()
		{
			userBalanceText.GetComponent<Animator>().Play("ScaleUp");

		}


	}
}