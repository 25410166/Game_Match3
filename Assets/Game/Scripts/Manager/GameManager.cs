using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum Turn
    {
        Player,
        AI
    }

    public Turn currentTurn = Turn.Player;
    private bool isProcessing = false;
    private bool gameEnded = false;
    private Coroutine turnTimerCoroutine;
    private bool aiMoveTriggered = false;
    private bool hasProcessedTurnEnd = false;

    [Header("References")]
    public PlayerStats player;
    public AIStats ai;

    [Header("Pets")]
    public PetBehaviour playerPet;
    public PetBehaviour aiPet;

    [Header("Turn Timer")]
    public float turnTransitionTime = 10f; // 10 giây cho mỗi lượt

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (player == null) player = FindObjectOfType<PlayerStats>();
        if (ai == null) ai = FindObjectOfType<AIStats>();
    }

    private void Start()
    {
        if (player != null) player.Init();
        if (ai != null) ai.Init();


        currentTurn = Turn.Player;
        Debug.Log("Lượt bắt đầu: Người chơi");
        turnTimerCoroutine = StartCoroutine(TurnTimer());
    }

    public void EndTurn(List<int> destroyedIds = null)
    {
        if (isProcessing || gameEnded)
        {
            Debug.Log("EndTurn bị chặn: isProcessing=" + isProcessing + ", gameEnded=" + gameEnded);
            return;
        }

        Debug.Log("Gọi EndTurn, currentTurn=" + currentTurn);
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
            if (UIManager.Instance != null)
                UIManager.Instance.StopTurnTimer();
        }
        StartCoroutine(EndTurnRoutine(destroyedIds));
    }

    private IEnumerator EndTurnRoutine(List<int> destroyedIds)
    {
        isProcessing = true;
        hasProcessedTurnEnd = true;
        Debug.Log("Bắt đầu EndTurnRoutine, currentTurn=" + currentTurn);

        if (destroyedIds != null && destroyedIds.Count > 0 && UIManager.Instance != null)
        {
            if (Board.Instance != null && Board.Instance.gameObject != null)
                Board.Instance.gameObject.SetActive(false);

            yield return StartCoroutine(UIManager.Instance.PlayDestroyedItemsSequence(destroyedIds));

            Dictionary<int, int> counts = new Dictionary<int, int>();
            foreach (int id in destroyedIds)
            {
                if (counts.ContainsKey(id)) counts[id]++;
                else counts[id] = 1;
            }

            if (currentTurn == Turn.Player && player != null)
            {
                foreach (var kv in counts)
                    player.ApplyEffect(kv.Key, kv.Value);
            }
            else if (currentTurn == Turn.AI && ai != null)
            {
                foreach (var kv in counts)
                    ai.ApplyEffect(kv.Key, kv.Value);
            }

            if (Board.Instance != null && Board.Instance.gameObject != null)
                Board.Instance.gameObject.SetActive(true);
        }

        if (CheckGameEnd())
        {
            isProcessing = false;
            hasProcessedTurnEnd = false;
            Debug.Log("Game kết thúc trong EndTurnRoutine");
            yield break;
        }

        // Đổi lượt
        currentTurn = (currentTurn == Turn.Player) ? Turn.AI : Turn.Player;
        Debug.Log("Lượt chuyển sang: " + currentTurn);

        hasProcessedTurnEnd = false;
        aiMoveTriggered = false;
        isProcessing = false;

        turnTimerCoroutine = StartCoroutine(TurnTimer());
    }

    private IEnumerator TurnTimer()
    {
        Debug.Log("Bắt đầu TurnTimer, currentTurn=" + currentTurn + ", aiMoveTriggered=" + aiMoveTriggered);

        if (currentTurn == Turn.AI && !gameEnded && !aiMoveTriggered)
        {
            aiMoveTriggered = true;
            Debug.Log("Gọi AIMove trong TurnTimer");
            StartCoroutine(AIMove());
        }

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowTurnTimer(turnTransitionTime, currentTurn));
        }
        else
        {
            yield return new WaitForSeconds(turnTransitionTime);
        }

        if (!isProcessing && !gameEnded && !hasProcessedTurnEnd)
        {
            Debug.Log("Hết thời gian, tự động gọi EndTurn");
            EndTurn(null);
        }
        else
        {
            Debug.Log("Không gọi EndTurn tự động vì isProcessing=" + isProcessing + ", gameEnded=" + gameEnded + ", hasProcessedTurnEnd=" + hasProcessedTurnEnd);
        }
    }

    private IEnumerator AIMove()
    {
        if (gameEnded)
        {
            Debug.Log("AIMove bị chặn vì gameEnded=true");
            yield break;
        }

        Debug.Log("Bắt đầu AIMove");
        yield return new WaitForSeconds(1f);

        AIController aiController = FindObjectOfType<AIController>();
        if (aiController != null)
        {
            Debug.Log("AIController tìm thấy, gọi MakeMove");
            yield return StartCoroutine(aiController.MakeMove());
            Debug.Log("AI hoàn thành MakeMove");
            // Không gọi EndTurn vì Board.CheckAndDestroyMatches đã xử lý
        }
        else
        {
            Debug.LogWarning("Không tìm thấy AIController trong scene!");
            if (!gameEnded && !isProcessing && !hasProcessedTurnEnd)
            {
                Debug.Log("Gọi EndTurn vì không tìm thấy AIController");
                EndTurn(null);
            }
        }
    }

    public bool CanPlayerMove()
    {
        bool canMove = currentTurn == Turn.Player && !isProcessing && !gameEnded;
        Debug.Log("CanPlayerMove: " + canMove);
        return canMove;
    }

    private bool CheckGameEnd()
    {
        if (player != null && player.HP <= 0)
        {
            Debug.Log("BẠN THUA! AI thắng.");
            gameEnded = true;
            return true;
        }

        if (ai != null && ai.Health <= 0)
        {
            Debug.Log("BẠN THẮNG! Người chơi thắng.");
            gameEnded = true;
            return true;
        }

        return false;
    }
}