using System;
using UnityEngine;

public enum KeyBindingAction
{
    MoveLeft,
    MoveRight,
    Jump,
    Aim,
    Shoot
}

public static class KeyBindingSettings
{
    private const string MoveLeftKey = "KeyBinding.MoveLeft";
    private const string MoveRightKey = "KeyBinding.MoveRight";
    private const string JumpKey = "KeyBinding.Jump";
    private const string AimKey = "KeyBinding.Aim";
    private const string ShootKey = "KeyBinding.Shoot";

    private static bool loaded;
    private static KeyCode moveLeft;
    private static KeyCode moveRight;
    private static KeyCode jump;
    private static KeyCode aim;
    private static KeyCode shoot;

    public static KeyCode MoveLeft
    {
        get
        {
            EnsureLoaded();
            return moveLeft;
        }
    }

    public static KeyCode MoveRight
    {
        get
        {
            EnsureLoaded();
            return moveRight;
        }
    }

    public static KeyCode Jump
    {
        get
        {
            EnsureLoaded();
            return jump;
        }
    }

    public static KeyCode Aim
    {
        get
        {
            EnsureLoaded();
            return aim;
        }
    }

    public static KeyCode Shoot
    {
        get
        {
            EnsureLoaded();
            return shoot;
        }
    }

    public static void EnsureLoaded()
    {
        if (loaded)
            return;

        moveLeft = LoadKey(MoveLeftKey, KeyCode.A);
        moveRight = LoadKey(MoveRightKey, KeyCode.D);
        jump = LoadKey(JumpKey, KeyCode.Space);
        aim = LoadKey(AimKey, KeyCode.Mouse1);
        shoot = LoadKey(ShootKey, KeyCode.Mouse0);
        loaded = true;
    }

    public static KeyCode GetKey(KeyBindingAction action)
    {
        EnsureLoaded();

        switch (action)
        {
            case KeyBindingAction.MoveLeft:
                return moveLeft;
            case KeyBindingAction.MoveRight:
                return moveRight;
            case KeyBindingAction.Jump:
                return jump;
            case KeyBindingAction.Aim:
                return aim;
            case KeyBindingAction.Shoot:
                return shoot;
            default:
                return KeyCode.None;
        }
    }

    public static bool SetKey(KeyBindingAction action, KeyCode key)
    {
        EnsureLoaded();

        if (!CanAssign(key))
            return false;

        KeyBindingAction duplicateAction;
        if (IsAssignedToOtherAction(action, key, out duplicateAction))
        {
            Debug.LogWarning(
                "[KeyBindingSettings] " + GetDisplayName(key) +
                " is already assigned to " + duplicateAction + ". Duplicate key binding was blocked."
            );
            return false;
        }

        switch (action)
        {
            case KeyBindingAction.MoveLeft:
                moveLeft = key;
                SaveKey(MoveLeftKey, key);
                break;
            case KeyBindingAction.MoveRight:
                moveRight = key;
                SaveKey(MoveRightKey, key);
                break;
            case KeyBindingAction.Jump:
                jump = key;
                SaveKey(JumpKey, key);
                break;
            case KeyBindingAction.Aim:
                aim = key;
                SaveKey(AimKey, key);
                break;
            case KeyBindingAction.Shoot:
                shoot = key;
                SaveKey(ShootKey, key);
                break;
            default:
                return false;
        }

        PlayerPrefs.Save();
        return true;
    }

    public static bool IsKeyHeld(KeyCode key)
    {
        if (IsMouseKey(key))
            return Input.GetMouseButton(GetMouseButtonIndex(key));

        return Input.GetKey(key);
    }

    public static bool IsKeyDown(KeyCode key)
    {
        if (IsMouseKey(key))
            return Input.GetMouseButtonDown(GetMouseButtonIndex(key));

        return Input.GetKeyDown(key);
    }

    public static bool TryGetPressedKey(out KeyCode key)
    {
        for (int mouseIndex = 0; mouseIndex <= 6; mouseIndex++)
        {
            if (Input.GetMouseButtonDown(mouseIndex))
            {
                key = (KeyCode)((int)KeyCode.Mouse0 + mouseIndex);
                return true;
            }
        }

        Array values = Enum.GetValues(typeof(KeyCode));
        for (int i = 0; i < values.Length; i++)
        {
            KeyCode candidate = (KeyCode)values.GetValue(i);

            if (candidate == KeyCode.None)
                continue;

            if (IsMouseKey(candidate))
                continue;

            if (Input.GetKeyDown(candidate))
            {
                key = candidate;
                return true;
            }
        }

        key = KeyCode.None;
        return false;
    }

    public static string GetDisplayName(KeyBindingAction action)
    {
        return GetDisplayName(GetKey(action));
    }

    public static string GetDisplayName(KeyCode key)
    {
        if (IsKeyCodeInRange(key, KeyCode.A, KeyCode.Z))
            return key.ToString().ToUpperInvariant();

        if (IsKeyCodeInRange(key, KeyCode.Alpha0, KeyCode.Alpha9))
            return ((int)key - (int)KeyCode.Alpha0).ToString();

        if (IsKeyCodeInRange(key, KeyCode.Keypad0, KeyCode.Keypad9))
            return "NUM " + ((int)key - (int)KeyCode.Keypad0);

        if (IsMouseKey(key))
            return "MOUSE " + GetMouseButtonIndex(key);

        if (key == KeyCode.Space)
            return "SPACE";

        return key.ToString().ToUpperInvariant();
    }

    private static KeyCode LoadKey(string prefsKey, KeyCode defaultKey)
    {
        return (KeyCode)PlayerPrefs.GetInt(prefsKey, (int)defaultKey);
    }

    private static void SaveKey(string prefsKey, KeyCode key)
    {
        PlayerPrefs.SetInt(prefsKey, (int)key);
    }

    private static bool CanAssign(KeyCode key)
    {
        if (key == KeyCode.None)
            return false;

        if (key == KeyCode.Escape)
            return false;

        return true;
    }

    private static bool IsAssignedToOtherAction(KeyBindingAction action, KeyCode key, out KeyBindingAction duplicateAction)
    {
        duplicateAction = action;

        KeyBindingAction[] actions =
        {
            KeyBindingAction.MoveLeft,
            KeyBindingAction.MoveRight,
            KeyBindingAction.Jump,
            KeyBindingAction.Aim,
            KeyBindingAction.Shoot
        };

        for (int i = 0; i < actions.Length; i++)
        {
            KeyBindingAction candidate = actions[i];
            if (candidate == action)
                continue;

            if (GetKey(candidate) == key)
            {
                duplicateAction = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool IsMouseKey(KeyCode key)
    {
        return IsKeyCodeInRange(key, KeyCode.Mouse0, KeyCode.Mouse6);
    }

    private static int GetMouseButtonIndex(KeyCode key)
    {
        return (int)key - (int)KeyCode.Mouse0;
    }

    private static bool IsKeyCodeInRange(KeyCode key, KeyCode min, KeyCode max)
    {
        int value = (int)key;
        return value >= (int)min && value <= (int)max;
    }
}
