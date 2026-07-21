#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using InnerNet;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
private static byte bugRoomAngelLastTargetId = byte.MaxValue;

private void TryBugRoomAutoAngelTick()
        {
            if (!bugRoomAutoAngel)
            {
                bugRoomAngelTimer = -1f;
                bugRoomAngelLastTargetId = byte.MaxValue;
                return;
            }

            if (!CanRunBugRoomTick()) return;

            PlayerControl local = PlayerControl.LocalPlayer;
            if (local == null || local.Data == null) return;

            if (!IsBugRoomLocalGuardianAngel(local))
            {
                bugRoomAngelTimer = -1f;
                bugRoomAngelLastTargetId = byte.MaxValue;
                return;
            }

            float now = Time.unscaledTime;
            if (bugRoomAngelTimer > 0f && now < bugRoomAngelTimer) return;
            bugRoomAngelTimer = now + Mathf.Clamp(bugRoomAutoAngelIntervalSeconds, 0.001f, 0.50f);

            PlayerControl target = PickBugRoomProtectTarget(local);
            if (target == null) return;

            if (TryClickBugRoomProtectButton())
            {
                bugRoomAngelLastTargetId = target.PlayerId;
            }
        }

private void TryBugRoomAutoKillShieldTick()
        {
            if (!bugRoomAutoKillShield)
            {
                bugRoomShieldKillTimer = -1f;
                return;
            }

            if (!CanRunBugRoomTick()) return;

            PlayerControl local = PlayerControl.LocalPlayer;
            if (local == null || local.Data == null || local.Data.IsDead || local.Data.Role == null) return;

            float now = Time.unscaledTime;
            if (bugRoomShieldKillTimer > 0f && now < bugRoomShieldKillTimer) return;
            bugRoomShieldKillTimer = now + 0.15f;

            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                if (!TryFindBugRoomHostShieldPair(out PlayerControl killer, out PlayerControl hostTarget)) return;

                try { killer.CmdCheckMurder(hostTarget); } catch { }
                return;
            }

            if (!IsBugRoomReadyKiller(local)) return;

            PlayerControl target = FindBugRoomShieldKillTarget(local);
            if (target == null) return;

            try { local.CmdCheckMurder(target); } catch { }
        }

private void TryBugRoomTimedAutoRunTick()
        {
            if (!bugRoomTimedAutoRun)
            {
                bugRoomTimedAutoRunTimer = 0f;
                bugRoomTimedAutoRunDone = false;
                return;
            }

            if (AutoHostAutoRunEnabled)
            {
                bugRoomTimedAutoRunTimer = 0f;
                bugRoomTimedAutoRunDone = true;
                return;
            }

            if (!IsBugRoomTimedAutoRunInGame())
            {
                bugRoomTimedAutoRunTimer = 0f;
                bugRoomTimedAutoRunDone = false;
                return;
            }

            if (bugRoomTimedAutoRunDone) return;

            bugRoomTimedAutoRunTimer += Time.deltaTime;
            if (bugRoomTimedAutoRunTimer < Mathf.Clamp(bugRoomTimedAutoRunMinutes, 1, 60) * 60f) return;

            AutoHostAutoRunEnabled = true;
            bugRoomTimedAutoRunDone = true;
            bugRoomTimedAutoRunTimer = 0f;
            settingsDirty = true;
            ShowNotification($"<color=#FF00FF>[BUG ROOM]</color> Auto Run {AutoHostAutoRunDelaySeconds:0.00}s enabled.");
        }

private static bool IsBugRoomTimedAutoRunInGame()
        {
            try
            {
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return false;
                if (ShipStatus.Instance == null || LobbyBehaviour.Instance != null) return false;
                if (UnityEngine.Object.FindObjectOfType<EndGameManager>() != null) return false;
                return true;
            }
            catch { return false; }
        }

private static bool CanRunBugRoomTick()
        {
            try
            {
                if (AmongUsClient.Instance == null) return false;
                if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return false;
                if (ShipStatus.Instance == null || LobbyBehaviour.Instance != null) return false;
                if (IsMeetingOrExileActive() || IntroCutscene.Instance != null) return false;
                return PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null;
            }
            catch { return false; }
        }

private static bool IsBugRoomAngelTarget(PlayerControl pc, PlayerControl local)
        {
            try
            {
                if (pc == null || pc == local || pc.Data == null) return false;
                if (pc.PlayerId >= 100 || pc.Data.Disconnected || pc.Data.IsDead) return false;
                if (pc.inVent || pc.onLadder || pc.inMovingPlat) return false;
                return pc.Visible;
            }
            catch { return false; }
        }

private static bool IsBugRoomLocalGuardianAngel(PlayerControl local)
        {
            try
            {
                return local.Data != null &&
                       !local.Data.Disconnected &&
                       local.Data.Role != null &&
                       local.Data.Role.Role == RoleTypes.GuardianAngel;
            }
            catch { return false; }
        }

private static PlayerControl PickBugRoomProtectTarget(PlayerControl local)
        {
            try
            {
                if (PlayerControl.AllPlayerControls == null) return null;

                List<PlayerControl> plrs = new List<PlayerControl>();
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (!IsBugRoomAngelTarget(pc, local)) continue;
                    plrs.Add(pc);
                }

                return PickBugRoomOtherTarget(plrs);
            }
            catch { return null; }
        }

private static PlayerControl PickBugRoomOtherTarget(List<PlayerControl> plrs)
        {
            if (plrs == null || plrs.Count == 0) return null;
            if (plrs.Count == 1) return plrs[0];

            for (int i = 0; i < 6; i++)
            {
                PlayerControl pc = plrs[UnityEngine.Random.Range(0, plrs.Count)];
                if (pc != null && pc.PlayerId != bugRoomAngelLastTargetId) return pc;
            }

            return plrs[UnityEngine.Random.Range(0, plrs.Count)];
        }

private static bool TryClickBugRoomProtectButton()
        {
            try
            {
                HudManager hud = DestroyableSingleton<HudManager>.Instance;
                if (hud == null || hud.AbilityButton == null) return false;

                object btn = hud.AbilityButton;
                if (TryClickBugRoomButtonObject(btn)) return true;

                Component cmp = btn as Component;
                if (cmp == null) return false;

                PassiveButton passive = cmp.GetComponent<PassiveButton>();
                if (ClickBugRoomPassiveButton(passive)) return true;

                PassiveButton[] kids = cmp.GetComponentsInChildren<PassiveButton>(true);
                if (kids != null)
                    foreach (PassiveButton child in kids)
                        if (ClickBugRoomPassiveButton(child))
                            return true;

                MonoBehaviour[] behaviours = cmp.gameObject.GetComponents<MonoBehaviour>();
                if (behaviours != null)
                    foreach (MonoBehaviour mb in behaviours)
                        if (mb != null && TryClickBugRoomButtonObject(mb))
                            return true;
            }
            catch { }

            return false;
        }

private static bool ClickBugRoomPassiveButton(PassiveButton btn)
        {
            if (btn == null) return false;
            bool clicked = false;

            try
            {
                if (btn.OnClick != null)
                {
                    btn.OnClick.Invoke();
                    clicked = true;
                }
            }
            catch { }

            try
            {
                btn.ReceiveClickDown();
                btn.ReceiveClickUp();
                clicked = true;
            }
            catch { }

            if (TryClickBugRoomButtonObject(btn)) clicked = true;
            return clicked;
        }

private static bool TryClickBugRoomButtonObject(object obj)
        {
            if (obj == null) return false;
            bool clicked = false;
            string[] names =
            {
                "DoClick", "Click", "OnClick", "PerformClick", "ReceiveClick",
                "ReceiveClickDown", "ReceiveClickUp", "Use", "UseAbility"
            };

            try
            {
                Type type = obj.GetType();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (string name in names)
                {
                    MethodInfo method = type.GetMethods(flags).FirstOrDefault(m => m.Name == name && m.GetParameters().Length == 0);
                    if (method == null) continue;

                    method.Invoke(obj, null);
                    clicked = true;
                }
            }
            catch { }

            return clicked;
        }

private static PlayerControl FindBugRoomAngelTarget(PlayerControl local)
        {
            try
            {
                if (local == null || PlayerControl.AllPlayerControls == null) return null;
                Vector3 lp = local.transform.position;
                PlayerControl best = null;
                float dist = float.MaxValue;

                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (!IsBugRoomAngelTarget(pc, local)) continue;

                    float d = Vector2.Distance(new Vector2(lp.x, lp.y), new Vector2(pc.transform.position.x, pc.transform.position.y));
                    if (d < dist)
                    {
                        dist = d;
                        best = pc;
                    }
                }
                return best;
            }
            catch { return null; }
        }

private static PlayerControl FindBugRoomShieldKillTarget(PlayerControl local)
        {
            try
            {
                if (local == null || local.Data == null || PlayerControl.AllPlayerControls == null) return null;

                Vector3 lp = local.transform.position;
                PlayerControl best = null;
                float dist = Mathf.Max(0.5f, GetVanillaKillDistance() + 0.25f);

                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (!IsBugRoomProtectedKillTarget(local, pc)) continue;

                    float d = Vector2.Distance(new Vector2(lp.x, lp.y), new Vector2(pc.transform.position.x, pc.transform.position.y));
                    if (d <= dist)
                    {
                        dist = d;
                        best = pc;
                    }
                }
                return best;
            }
            catch { return null; }
        }

private static bool TryFindBugRoomHostShieldPair(out PlayerControl killer, out PlayerControl target)
        {
            killer = null;
            target = null;

            try
            {
                if (PlayerControl.AllPlayerControls == null) return false;

                float best = float.MaxValue;
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (!IsBugRoomReadyKiller(pc)) continue;

                    PlayerControl t = FindBugRoomShieldKillTarget(pc);
                    if (t == null) continue;

                    float d = Vector2.Distance(pc.transform.position, t.transform.position);
                    if (d >= best) continue;

                    best = d;
                    killer = pc;
                    target = t;
                }
            }
            catch { return false; }

            return killer != null && target != null;
        }

private static bool IsBugRoomReadyKiller(PlayerControl pc)
        {
            try
            {
                if (pc == null || pc.Data == null) return false;
                if (pc.Data.Disconnected || pc.Data.IsDead) return false;
                bool canKill = pc.Data.Role != null && pc.Data.Role.CanUseKillButton;
                bool imp = false;
                try { imp = pc.Data.Role != null && pc.Data.Role.IsImpostor; } catch { }
                try { imp = imp || RoleManager.IsImpostorRole(pc.Data.RoleType); } catch { }
                if (!canKill || !imp) return false;
                if (pc.inVent || pc.onLadder || pc.inMovingPlat) return false;
                return Mathf.Max(0f, pc.killTimer) <= 0.05f;
            }
            catch { return false; }
        }

private static bool IsBugRoomProtectedKillTarget(PlayerControl local, PlayerControl target)
        {
            try
            {
                if (local == null || target == null || target.Data == null) return false;
                if (target.PlayerId == local.PlayerId || target.PlayerId >= 100) return false;
                if (target.Data.Disconnected || target.Data.IsDead) return false;
                if (target.protectedByGuardianId < 0) return false;
                if (!target.Visible || target.inVent || target.onLadder || target.inMovingPlat) return false;
                if (target.Data.Role == null || !target.Data.Role.CanBeKilled) return false;
                return true;
            }
            catch { return false; }
        }

private static List<PlayerControl> GetBugRoomKillTargets()
        {
            List<PlayerControl> plrs = new List<PlayerControl>();
            try
            {
                if (PlayerControl.AllPlayerControls == null) return plrs;
                PlayerControl local = PlayerControl.LocalPlayer;
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == local || pc.Data == null) continue;
                    if (pc.Data.Disconnected || pc.PlayerId >= 100) continue;
                    plrs.Add(pc);
                }
                plrs.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
            }
            catch { }
            return plrs;
        }
    }
}
