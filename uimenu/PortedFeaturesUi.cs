#nullable disable
using System.Linq;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        private Vector2 petHandPlayerScroll = Vector2.zero;

        private void DrawPetHandTab()
        {
            float contentWidth = GetMenuWorkWidth(220f, 760f);
            bool compact = contentWidth < 430f;
            float gap = 8f;
            float playerWidth = compact ? contentWidth : Mathf.Clamp(contentWidth * 0.32f, 170f, 220f);
            float controlsWidth = compact ? contentWidth : contentWidth - playerWidth - gap;

            if (compact) GUILayout.BeginVertical(GUILayout.Width(contentWidth));
            else GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(playerWidth), compact ? GUILayout.Height(130f) : GUILayout.ExpandHeight(true));
            DrawMenuSectionHeader("PET TARGET");
            petHandPlayerScroll = GUILayout.BeginScrollView(petHandPlayerScroll, false, true,
                GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
            try
            {
                foreach (PlayerControl player in lockedPlayersList)
                {
                    if (player == null || player.Data == null || player.Data.Disconnected || player.PlayerId >= 100) continue;
                    bool selected = selectedAntiCheatPlayerId == player.PlayerId;

                    GUI.contentColor = Color.white;
                    try { GUI.contentColor = Palette.PlayerColors[player.Data.DefaultOutfit.ColorId]; } catch { }
                    if (GUILayout.Button(player.Data.PlayerName ?? "Unknown", selected ? activeTabStyle : btnStyle, GUILayout.Height(28)))
                        selectedAntiCheatPlayerId = player.PlayerId;
                    GUI.contentColor = Color.white;
                }
            }
            finally { GUILayout.EndScrollView(); }
            GUILayout.EndVertical();

            if (compact) GUILayout.Space(gap); else GUILayout.Space(gap);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(controlsWidth), GUILayout.ExpandHeight(true));
            DrawMenuSectionHeader("PET HAND CONTROL");

            PlayerControl target = lockedPlayersList.FirstOrDefault(player =>
                player != null && player.Data != null && player.PlayerId == selectedAntiCheatPlayerId);

            if (target != null)
                GUILayout.Label($"<color=#AAAAAA>Target:</color> {target.Data.PlayerName}", richLabelStyle12);
            else
                GUILayout.Label("<color=#777777>Select a player for PET PLAYER.</color>", richLabelStyle11);

            GUILayout.Space(6);
            float innerWidth = Mathf.Max(170f, controlsWidth - 34f);
            float pairGap = 6f;
            float halfWidth = Mathf.Floor((innerWidth - pairGap) * 0.5f);

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            GUI.enabled = target != null;
            if (GUILayout.Button(L("PET PLAYER", "ГЛАДИТЬ ИГРОКА"),
                target != null && PetControl.IsTarget(target.PlayerId) ? activeTabStyle : btnStyle,
                GUILayout.Width(halfWidth), GUILayout.Height(27)))
            {
                ShowNotification("<color=#00FF00>[PET]</color> " + PetControl.Grab(target));
            }
            GUI.enabled = true;
            GUILayout.Space(pairGap);
            if (GUILayout.Button(L("STOP", "СТОП"), btnStyle, GUILayout.Width(halfWidth), GUILayout.Height(27)))
                ShowNotification("<color=#00FF00>[PET]</color> " + PetControl.StopPet());
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            if (GUILayout.Button(L("MANUAL JOYSTICK", "РУЧНОЙ ДЖОЙСТИК"),
                PetControl.On && PetControl.Manual ? activeTabStyle : btnStyle,
                GUILayout.Width(halfWidth), GUILayout.Height(27)))
            {
                ShowNotification("<color=#00FF00>[PET]</color> " + PetControl.ToggleManual());
            }
            GUILayout.Space(pairGap);
            if (GUILayout.Button(L("PAINT", "РОСПИСЬ"),
                PetControl.On && PetControl.Paint ? activeTabStyle : btnStyle,
                GUILayout.Width(halfWidth), GUILayout.Height(27)))
            {
                ShowNotification("<color=#00FF00>[PET]</color> " + PetControl.TogglePaint());
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            DrawMenuSectionHeader("ARROW CONTROL");
            float arrowSize = 34f;
            float arrowGap = 4f;

            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            GUILayout.FlexibleSpace();
            if (GUILayout.RepeatButton("▲", PetControl.On && PetControl.Manual ? activeTabStyle : btnStyle,
                GUILayout.Width(arrowSize), GUILayout.Height(27f)))
                PetControl.SetArrowInput(Vector2.up);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(arrowGap);
            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            GUILayout.FlexibleSpace();
            if (GUILayout.RepeatButton("◀", PetControl.On && PetControl.Manual ? activeTabStyle : btnStyle,
                GUILayout.Width(arrowSize), GUILayout.Height(27f)))
                PetControl.SetArrowInput(Vector2.left);
            GUILayout.Space(arrowGap);
            if (GUILayout.Button("●", btnStyle, GUILayout.Width(arrowSize), GUILayout.Height(27f)))
                PetControl.CenterManual();
            GUILayout.Space(arrowGap);
            if (GUILayout.RepeatButton("▶", PetControl.On && PetControl.Manual ? activeTabStyle : btnStyle,
                GUILayout.Width(arrowSize), GUILayout.Height(27f)))
                PetControl.SetArrowInput(Vector2.right);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(arrowGap);
            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            GUILayout.FlexibleSpace();
            if (GUILayout.RepeatButton("▼", PetControl.On && PetControl.Manual ? activeTabStyle : btnStyle,
                GUILayout.Width(arrowSize), GUILayout.Height(27f)))
                PetControl.SetArrowInput(Vector2.down);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            DrawMenuSectionHeader("ROOM FILL");
            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) PetControl.RoomStep(-1);
            GUILayout.Label(PetControl.RoomName(), accentValueStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) PetControl.RoomStep(1);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal(GUILayout.Width(innerWidth));
            if (GUILayout.Button(L("FILL ROOM", "ЗАЛИТЬ КОМНАТУ"), btnStyle, GUILayout.Width(halfWidth), GUILayout.Height(27)))
                ShowNotification("<color=#00FF00>[PET]</color> " + PetControl.FillRoom());
            GUILayout.Space(pairGap);
            if (GUILayout.Button($"{L("CLEAR", "ОЧИСТИТЬ")} ({PetControl.PaintCount})", btnStyle,
                GUILayout.Width(halfWidth), GUILayout.Height(27)))
            {
                ShowNotification("<color=#00FF00>[PET]</color> " + PetControl.ClearPaint());
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("<color=#777777>Manual mode uses the on-screen joystick. Paint mode records the mouse path while the menu is closed.</color>", richLabelStyle11);
            GUILayout.EndVertical();

            if (compact) GUILayout.EndVertical();
            else GUILayout.EndHorizontal();
        }

        private void DrawTargetVisionControls(PlayerControl target, float contentWidth)
        {
            if (target == null || target.Data == null) return;

            GUILayout.Space(10);
            DrawMenuSectionHeader("TARGET VISION");
            GUILayout.Label($"<color=#AAAAAA>State:</color> {TargetVisionFeature.StateName(target.PlayerId)}", richLabelStyle11);
            GUILayout.Space(3);

            float gap = 5f;
            float buttonWidth = Mathf.Floor((contentWidth - gap * 2f) / 3f);
            GUILayout.BeginHorizontal(GUILayout.Width(contentWidth));

            if (DrawFixedMenuButton("BLIND", btnStyle, buttonWidth, 24f))
                ShowNotification("<color=#FFAA44>[VISION]</color> " + TargetVisionFeature.Blind(target));

            GUILayout.Space(gap);
            if (DrawFixedMenuButton("FULLBRIGHT", activeTabStyle, buttonWidth, 24f))
                ShowNotification("<color=#66DDFF>[VISION]</color> " + TargetVisionFeature.FullBright(target));

            GUILayout.Space(gap);
            if (DrawFixedMenuButton("RESTORE", btnStyle, buttonWidth, 24f))
                ShowNotification("<color=#AAFFAA>[VISION]</color> " + TargetVisionFeature.Restore(target));

            GUILayout.EndHorizontal();
        }
    }
}
