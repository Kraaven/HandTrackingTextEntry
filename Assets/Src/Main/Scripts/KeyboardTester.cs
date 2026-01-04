using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class KeyboardTester : MonoBehaviour
{
    private Keyboard keyboard;

    private void OnEnable()
    {
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        if (keyboard == null)
            return;

        // ================================
        // START SESSION
        // ================================
        if (keyboard.enterKey.wasPressedThisFrame)
        {
            GameManager.Instance.StartSession();
            Debug.Log("Session started");
        }

        // ================================
        // BACKSPACE
        // ================================
        if (keyboard.backspaceKey.wasPressedThisFrame)
        {
            GameManager.Instance.DeleteCharacter();
        }

        // ================================
        // CHARACTER INPUT
        // ================================
        foreach (var key in keyboard.allKeys)
        {
            if (!key.wasPressedThisFrame)
                continue;

            char c;
            if (TryGetCharFromKey(key, out c))
            {
                GameManager.Instance.InsertCharacter(c);
            }
        }
    }

    private bool TryGetCharFromKey(KeyControl key, out char character)
    {
        character = '\0';

        // Ignore control keys
        if (key.keyCode < Key.Space || key.keyCode > Key.Z)
            return false;

        bool shift =
            keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed;

        character = key.keyCode switch
        {
            Key.A => shift ? 'A' : 'a',
            Key.B => shift ? 'B' : 'b',
            Key.C => shift ? 'C' : 'c',
            Key.D => shift ? 'D' : 'd',
            Key.E => shift ? 'E' : 'e',
            Key.F => shift ? 'F' : 'f',
            Key.G => shift ? 'G' : 'g',
            Key.H => shift ? 'H' : 'h',
            Key.I => shift ? 'I' : 'i',
            Key.J => shift ? 'J' : 'j',
            Key.K => shift ? 'K' : 'k',
            Key.L => shift ? 'L' : 'l',
            Key.M => shift ? 'M' : 'm',
            Key.N => shift ? 'N' : 'n',
            Key.O => shift ? 'O' : 'o',
            Key.P => shift ? 'P' : 'p',
            Key.Q => shift ? 'Q' : 'q',
            Key.R => shift ? 'R' : 'r',
            Key.S => shift ? 'S' : 's',
            Key.T => shift ? 'T' : 't',
            Key.U => shift ? 'U' : 'u',
            Key.V => shift ? 'V' : 'v',
            Key.W => shift ? 'W' : 'w',
            Key.X => shift ? 'X' : 'x',
            Key.Y => shift ? 'Y' : 'y',
            Key.Z => shift ? 'Z' : 'z',

            Key.Space => ' ',

            Key.Digit0 => shift ? ')' : '0',
            Key.Digit1 => shift ? '!' : '1',
            Key.Digit2 => shift ? '@' : '2',
            Key.Digit3 => shift ? '#' : '3',
            Key.Digit4 => shift ? '$' : '4',
            Key.Digit5 => shift ? '%' : '5',
            Key.Digit6 => shift ? '^' : '6',
            Key.Digit7 => shift ? '&' : '7',
            Key.Digit8 => shift ? '*' : '8',
            Key.Digit9 => shift ? '(' : '9',

            _ => '\0'
        };

        return character != '\0';
    }
}
