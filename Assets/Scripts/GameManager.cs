using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO; // For file operations
using System;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;
    public static int gameSize = 2;
    // gameobject instance
    [SerializeField]
    private GameObject prefab;
    // parent object of cards
    [SerializeField]
    private GameObject cardList;
    // sprite for card back
    [SerializeField]
    private Sprite cardBack;
    // all possible sprite for card front
    [SerializeField]
    private Sprite[] sprites;
    // list of card
    private Card[] cards;

    //we place card on this panel
    [SerializeField]
    private GameObject gamePanel;
    [SerializeField]
    private GameObject info;
    [SerializeField]
    private GameObject menuPanel;
    // for preloading
    [SerializeField]
    private Card spritePreload;
    // other UI
    [SerializeField]
    private TextMeshProUGUI sizeLabel;
    [SerializeField]
    private Slider sizeSlider;
    [SerializeField]
    private TextMeshProUGUI timeLabel;
    private float time;
    [SerializeField]
    private TextMeshProUGUI turnLabel;
    private int turn;
    [SerializeField]
    private TextMeshProUGUI matchLabel;
    private int match;

    private int spriteSelected;
    private int cardSelected;
    private int cardLeft;
    private bool gameStart;

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        gameStart = false;
        gamePanel.SetActive(false);
        menuPanel.SetActive(true);
        info.SetActive(false);
        // Preload card images
        PreloadCardImage();

        LoadGame(); // Try to load saved game on start
    }


    // Purpose is to allow preloading of panel, so that it does not lag when it loads
    // Call this in the start method to preload all sprites at start of the script
    private void PreloadCardImage()
    {
        for (int i = 0; i < sprites.Length; i++)
            spritePreload.SpriteID = i;
        spritePreload.gameObject.SetActive(false);
    }
    // Start a game
    public void StartCardGame()
    {
        if (gameStart) return; // return if game already running
        gameStart = true;
        // toggle UI
        gamePanel.SetActive(true);
        menuPanel.SetActive(false);
        info.SetActive(false);
        // set cards, size, position
        SetGamePanel();
        // renew gameplay variables
        cardSelected = spriteSelected = -1;
        cardLeft = cards.Length;
        // allocate sprite to card
        SpriteCardAllocation();
        StartCoroutine(HideFace());
        time = 0;
        match = turn = 0;
         
    }

    // Initialize cards, size, and position based on size of game
    private void SetGamePanel(){
        // if game is odd, we should have 1 card less
        int isOdd = gameSize % 2 ;

        cards = new Card[gameSize * gameSize - isOdd];
        // remove all gameobject from parent
        foreach (Transform child in cardList.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        // calculate position between each card & start position of each card based on the Panel
        RectTransform panelsize = gamePanel.transform.GetComponent(typeof(RectTransform)) as RectTransform;
        float row_size = panelsize.sizeDelta.x;
        float col_size = panelsize.sizeDelta.y;
        float scale = 1.0f/gameSize;
        float xInc = row_size/gameSize;
        float yInc = col_size/gameSize;
        float curX = -xInc * (float)(gameSize / 2);
        float curY = -yInc * (float)(gameSize / 2);

        if(isOdd == 0) {
            curX += xInc / 2;
            curY += yInc / 2;
        }
        float initialX = curX;
        // for each in y-axis
        for (int i = 0; i < gameSize; i++)
        {
            curX = initialX;
            // for each in x-axis
            for (int j = 0; j < gameSize; j++)
            {
                GameObject c;
                // if is the last card and game is odd, we instead move the middle card on the panel to last spot
                if (isOdd == 1 && i == (gameSize - 1) && j == (gameSize - 1))
                {
                    int index = gameSize / 2 * gameSize + gameSize / 2;
                    c = cards[index].gameObject;
                }
                else
                {
                    // create card prefab
                    c = Instantiate(prefab);
                    // assign parent
                    c.transform.parent = cardList.transform;

                    int index = i * gameSize + j;
                    cards[index] = c.GetComponent<Card>();
                    cards[index].ID = index;
                    // modify its size
                    c.transform.localScale = new Vector3(scale, scale);
                }
                // assign location
                c.transform.localPosition = new Vector3(curX, curY, 0);
                curX += xInc;

            }
            curY += yInc;
        }

    }
    // reset face-down rotation of all cards
    void ResetFace()
    {
        for (int i = 0; i < gameSize; i++)
            cards[i].ResetRotation();
    }
    // Flip all cards after a short period
    IEnumerator HideFace()
    {
        //display for a short moment before flipping
        yield return new WaitForSeconds(0.3f);
        for (int i = 0; i < cards.Length; i++)
            cards[i].Flip();
        yield return new WaitForSeconds(0.5f);
    }
    // Allocate pairs of sprite to card instances
    private void SpriteCardAllocation()
    {
        int i, j;
        int[] selectedID = new int[cards.Length / 2];
        // sprite selection
        for (i = 0; i < cards.Length/2; i++)
        {
            // get a random sprite
            int value = UnityEngine.Random.Range(0, sprites.Length - 1);
            // check previous number has not been selection
            // if the number of cards is larger than number of sprites, it will reuse some sprites
            for (j = i; j > 0; j--)
            {
                if (selectedID[j - 1] == value)
                    value = (value + 1) % sprites.Length;
            }
            selectedID[i] = value;
        }

        // card sprite deallocation
        for (i = 0; i < cards.Length; i++)
        {
            cards[i].Active();
            cards[i].SpriteID = -1;
            cards[i].ResetRotation();
        }
        // card sprite pairing allocation
        for (i = 0; i < cards.Length / 2; i++)
            for (j = 0; j < 2; j++)
            {
                int value = UnityEngine.Random.Range(0, cards.Length - 1);
                while (cards[value].SpriteID != -1)
                    value = (value + 1) % cards.Length;

                cards[value].SpriteID = selectedID[i];
            }

    }
    // Slider update gameSize
    public void SetGameSize() {
        gameSize = (int)sizeSlider.value;
        sizeLabel.text = gameSize + " X " + gameSize;
    }
    // return Sprite based on its id
    public Sprite GetSprite(int spriteId)
    {
        return sprites[spriteId];
    }
    // return card back Sprite
    public Sprite CardBack()
    {
        return cardBack;
    }
    // check if clickable
    public bool canClick()
    {
        if (!gameStart)
            return false;
        return true;
    }
    // card onclick event
    public void cardClicked(int spriteId, int cardId)
    {
        // first card selected
        if (spriteSelected == -1)
        {
            spriteSelected = spriteId;
            cardSelected = cardId;
        }
        else
        { // second card selected
            if (spriteSelected == spriteId)
            {
                //correctly matched
                cards[cardSelected].Inactive();
                cards[cardId].Inactive();
                cardLeft -= 2;
                match++;
                matchLabel.text = match.ToString();
                CheckGameWin();
            }
            else
            {
                // incorrectly matched
                cards[cardSelected].Flip();
                cards[cardId].Flip();
            }
            turn++;
            turnLabel.text = turn.ToString();
            cardSelected = spriteSelected = -1;
        }
    }
    // check if game is completed
    private void CheckGameWin()
    {
        // win game
        if (cardLeft == 0)
        {
            info.SetActive(true);
            EndGame();
            AudioPlayer.Instance.PlayAudio(1);
        }
    }
    // stop game
    private void EndGame()
    {
        gameStart = false;
        gamePanel.SetActive(false);
        menuPanel.SetActive(true);
    }
    public void GiveUp()
    {
        EndGame();
    }
    public void DisplayInfo(bool i)
    {
        info.SetActive(i);
    }
    // track elasped time
    private void Update(){
        if (gameStart) {
            time += Time.deltaTime;
            timeLabel.text = (int)time + "s";

        }
    }
    private string saveFileName = "memory_game_save.json"; // Save file name

    // Save game data to a JSON file
    public bool SaveGame()
    {
        try
        {
            SaveData saveData = new SaveData(gameSize, cardSelected, spriteSelected, cardLeft, match, turn, time, cards);
            // ... Convert saveData object to JSON string ...
            string jsonData = JsonUtility.ToJson(saveData);
            File.WriteAllText(Application.persistentDataPath + "/" + saveFileName, jsonData);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
            return false;
        }
    }

    // Load game data from a JSON file
    public bool LoadGame()
    {
        try
        {
            string jsonData = File.ReadAllText(Application.persistentDataPath + "/" + saveFileName);
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);
            gameSize = saveData.gameSize;
            cardSelected = saveData.cardSelected;
            spriteSelected = saveData.spriteSelected;
            cardLeft = saveData.cardLeft;
            match = saveData.match;
            turn = saveData.turn;
            time = saveData.time;
            // ... (Load card states from saveData.cardStates) ...
            return true;
        }
        catch (Exception e)
        {
            Debug.Log("Load failed: " + e.Message);
            return false;
        }
    }
    // Load card states from the save data
    private void LoadCardStates(List<CardState> cardStates)
    {
        if (cardStates == null || cardStates.Count != cards.Length)
        {
            Debug.LogError("Invalid card state data");
            return;
        }
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].flipped = cardStates[i].flipped;
            cards[i].SpriteID = cardStates[i].spriteID; // Set the sprite ID for each card based on save data
        }
    }
}

// Class to store save data
public class SaveData
{
    public int gameSize;
    public int cardSelected;
    public int spriteSelected;
    public int cardLeft;
    public int match;
    public int turn;
    public float time;

    // Add a new field to store card states
    public List<CardState> cardStates;

    public SaveData(int gameSize, int cardSelected, int spriteSelected, int cardLeft, int match, int turn, float time, Card[] cards)
    {
        this.gameSize = gameSize;
        this.cardSelected = cardSelected;
        this.spriteSelected = spriteSelected;
        this.cardLeft = cardLeft;
        this.match = match;
        this.turn = turn;
        this.time = time;
        cardStates = new List<CardState>();
        foreach (Card card in cards)
        {
            cardStates.Add(new CardState(card.flipped, card.SpriteID));
        }
    }
}

// Class to store the state of a single card for saving
public class CardState
{
    public bool flipped;
    public int spriteID;

    public CardState(bool flipped, int spriteID)
    {
        this.flipped = flipped;
        this.spriteID = spriteID;
    }
}

