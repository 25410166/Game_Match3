UI RULES (STRICT):

- Always use TextMeshProUGUI directly
- Do NOT create wrapper classes for text
- Do NOT create unnecessary UI controllers
- Prefer:
    public TextMeshProUGUI damageText;

- Avoid:
    TextManager, TextWrapper, UIAdapter

- Keep UI scripts SHORT and DIRECT

- If a script >100 lines for simple UI → simplify it

Goal:
Minimal, readable, direct UI code