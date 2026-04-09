using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using TMPro;
using System.Collections;

public class AIStats : MonoBehaviour
{
    [Header("Pet Config (Tự động)")]
    public int petId = -1;
    public int level = 1;
    public GameObject AI;

    [Header("Max Stats")]
    public int maxHealth = 300;
    public int maxMana = 200;
    public int maxRage = 100;

    public int armor;
    public int baseAttack;
    public float critRate;
    public float critDamage;
    public string weakness;
    public AttackType attackType;

    public int Health { get; private set; }
    public int Mana { get; private set; }
    public int Rage { get; private set; }

    [Header("UI References")]
    public Image healthSlider;
    public TextMeshProUGUI _txtHP;
    public Image manaSlider;
    public TextMeshProUGUI _txtMN;
    public Image rageSlider;
    public TextMeshProUGUI _txtRG;

    [Header("Spine Animation")]
    public SkeletonAnimation skeletonAnim;
    private string currentAnim;

    private bool isInitialized = false;

    private void Awake()
    {
        StartCoroutine(AutoLoadPetData());
    }

    private IEnumerator AutoLoadPetData()
    {
        yield return new WaitForEndOfFrame();

        if (skeletonAnim != null)
        {
            var holder = skeletonAnim.gameObject.GetComponentInParent<PetStatsHolder>();
            if (holder != null)
            {
                petId = holder.petId;
                LoadPetData(holder);
            }
        }

        if (!isInitialized)
        {
            Init();
            isInitialized = true;
        }
    }

    private void LoadPetData(PetStatsHolder holder)
    {
        var data = holder.GetLevelData(level);
        if (data == null)
        {
            Debug.LogError($"<color=red>[AIStats] Pet ID {holder.petId} không có level {level}</color>");
            return;
        }

        maxHealth = data.baseHP;
        maxMana = data.baseMana;
        maxRage = data.baseRage;
        armor = data.armor;
        baseAttack = data.baseAttack;
        critRate = data.critRate;
        critDamage = data.critDamage;
        weakness = data.weakness;
        attackType = data.attackType;

        Debug.Log($"<color=orange>AI PET LOADED: {holder.petName} (ID {petId}) LV{level}</color>");
    }

    public void Init()
    {
        Health = maxHealth;
        Mana = 0;
        Rage = 0;
        UpdateUI();
        PlayIdle();
    }

    public void ApplyEffect(int itemId, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            if (itemId == 4) Attack();
        }
        UpdateUI();
    }

    public void Heal(int amount)
    {
        Health = Mathf.Min(Health + amount, maxHealth);
        UpdateUI();
    }

    public void GainMana(int amount)
    {
        Mana = Mathf.Min(Mana + amount, maxMana);
        UpdateUI();
    }

    public void GainRage(int amount)
    {
        Rage = Mathf.Min(Rage + amount, maxRage);
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        int finalDmg = Mathf.Max(1, amount - armor);
        Health = Mathf.Max(Health - finalDmg, 0);
        UpdateUI();
        if (Health <= 0) PlayDead();
    }

    private void DealDamageToPlayer(int amount)
    {
        GameManager.Instance?.player?.TakeDamage(amount);
    }

    private void UpdateUI()
    {
        if (healthSlider) healthSlider.fillAmount = (float)Health / maxHealth;
        if (manaSlider) manaSlider.fillAmount = (float)Mana / maxMana;
        if (rageSlider) rageSlider.fillAmount = (float)Rage / maxRage;

        if (_txtHP) _txtHP.text = $"{maxHealth} / {Health}";
        if (_txtMN) _txtMN.text = $"{maxMana} / {Mana}";
        if (_txtRG) _txtRG.text = $"{maxRage} / {Rage}";
    }

    // ================= SPINE + ATTACK =================
    public void Attack()
    {
        if (attackType == AttackType.Melee)
            StartCoroutine(MeleeAttackRoutine());
        else
            StartCoroutine(RangedAttackRoutine());
    }

    private IEnumerator MeleeAttackRoutine()
    {
        Transform model = skeletonAnim.transform;
        Vector3 originalPos = model.position;
        Vector3 attackPos = new Vector3(GameManager.Instance.player.skeletonAnim.transform.position.x + 1.2f, originalPos.y, originalPos.z);
        float moveSpeed = 6f;

        yield return StartCoroutine(MoveToPosition(model, attackPos, moveSpeed));
        SetAnim("Attack", false);
        yield return new WaitForSeconds(1.2f);

        DealDamageToPlayer(baseAttack);

        yield return StartCoroutine(MoveToPosition(model, originalPos, moveSpeed));
        PlayIdle();
    }

    private IEnumerator RangedAttackRoutine()
    {
        SetAnim("Attack", false);
        yield return new WaitForSeconds(1.2f);
        DealDamageToPlayer(baseAttack);
        PlayIdle();
    }

    private IEnumerator MoveToPosition(Transform model, Vector3 targetPos, float speed)
    {
        while (Vector3.Distance(model.position, targetPos) > 0.01f)
        {
            model.position = Vector3.MoveTowards(model.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
    }

    public void PlayDead() => SetAnim("Dead", false);
    public void PlayIdle() => SetAnim("Idle", true);

    private void SetAnim(string n, bool l)
    {
        if (skeletonAnim && currentAnim != n)
        {
            skeletonAnim.state.SetAnimation(0, n, l);
            currentAnim = n;
        }
    }
}
