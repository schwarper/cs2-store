# CS2 Store - Daily Gameplay Credits Cap

## English (Player Info)
### What is this?
We added a daily cap for gameplay-earned Store credits.

### How it works
- Only automatic gameplay rewards are capped.
- Manual/admin/website/API credits are not capped.
- Negative credits (penalties) still apply normally.
- If a reward would exceed the cap, you receive only the remaining amount.
- The cap resets automatically after the configured time window.

### Example
- Daily cap: `70`
- You already earned: `68`
- New gameplay reward: `+5`
- You receive: `+2` (to reach `70/70`)

### Commands
- Player: `!capstatus` (or `css_capstatus`)
- Admin: `css_capreset <player|steamid|target>`

### Config (server-side)
```toml
[DailyEarnedCreditsCap]
Enabled = true
MaxCreditsPerDay = 70
ResetEveryHours = 24
```

This helps prevent farming and keeps the server economy balanced.

---

## Български (Информация за играчи)
### Какво е това?
Добавихме дневен лимит за автоматично спечелени Store credits от gameplay.

### Как работи
- Лимитът важи само за автоматични gameplay награди.
- Ръчни/admin/website/API кредити не се лимитират.
- Минус кредити (наказания) се прилагат нормално.
- Ако наградата ще надвиши лимита, получаваш само оставащите кредити.
- Лимитът се нулира автоматично след зададения период.

### Пример
- Дневен лимит: `70`
- Вече спечелени: `68`
- Нова gameplay награда: `+5`
- Реално получаваш: `+2` (до `70/70`)

### Команди
- Играч: `!capstatus` (или `css_capstatus`)
- Админ: `css_capreset <player|steamid|target>`

### Конфиг (за сървъра)
```toml
[DailyEarnedCreditsCap]
Enabled = true
MaxCreditsPerDay = 70
ResetEveryHours = 24
```

Това ограничава farming-а и пази икономиката на сървъра по-балансирана.
