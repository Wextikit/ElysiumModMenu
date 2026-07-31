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

private const float bugRoomImpMeetingDelay = 20f;

private static readonly HashSet<byte> glitchRoomProtectedPlayers = new HashSet<byte>();

private static readonly List<byte> glitchRoomProtectionRemove = new List<byte>();

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

private void TryBugRoomImpMeetingTick()
        {
            if (!bugRoomImpMeeting)
            {
                bugRoomImpMeetingTimer = 0f;
                bugRoomImpMeetingDone = false;
                return;
            }

            AmongUsClient client = AmongUsClient.Instance;
            if (client == null ||
                client.GameState != InnerNetClient.GameStates.Started ||
                ShipStatus.Instance == null ||
                LobbyBehaviour.Instance != null)
            {
                bugRoomImpMeetingTimer = 0f;
                bugRoomImpMeetingDone = false;
                return;
            }

            if (client.AmHost)
            {
                bugRoomImpMeetingTimer = 0f;
                bugRoomImpMeetingDone = false;
                return;
            }

            PlayerControl local = PlayerControl.LocalPlayer;
            if (local == null || local.Data == null || local.Data.Disconnected || local.Data.IsDead || local.Data.Role == null)
            {
                bugRoomImpMeetingTimer = 0f;
                return;
            }

            bool imp = false;
            try { imp = local.Data.Role.IsImpostor; } catch { }
            try { imp = imp || RoleManager.IsImpostorRole(local.Data.RoleType); } catch { }
            if (!imp)
            {
                bugRoomImpMeetingTimer = 0f;
                bugRoomImpMeetingDone = false;
                return;
            }

            if (bugRoomImpMeetingDone) return;
            if (IsMeetingOrExileActive())
            {
                bugRoomImpMeetingTimer = 0f;
                bugRoomImpMeetingDone = true;
                return;
            }
            if (IntroCutscene.Instance != null) return;

            bugRoomImpMeetingTimer += Time.deltaTime;
            if (bugRoomImpMeetingTimer < bugRoomImpMeetingDelay) return;

            bugRoomImpMeetingTimer = 0f;
            bugRoomImpMeetingDone = true;
            callMeetingPublic();
        }

private void TryGlitchRoomGodModeTick()
        {
            if (!glitchRoomGodMode)
            {
                glitchRoomGodModeTimer = -1f;
                return;
            }

            if (!CanRunBugRoomTick()) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            PlayerControl local = PlayerControl.LocalPlayer;
            if (local == null || local.Data == null || local.Data.Disconnected || local.Data.IsDead) return;
            if (local.protectedByGuardianId >= 0) return;

            float now = Time.unscaledTime;
            if (glitchRoomGodModeTimer > 0f && now < glitchRoomGodModeTimer) return;
            glitchRoomGodModeTimer = now + 0.20f;

            try { local.RpcProtectPlayer(local, local.Data.DefaultOutfit.ColorId); } catch { }
        }

private void TryGlitchRoomGodModeAllTick()
        {
            if (!glitchRoomGodModeAll)
            {
                glitchRoomGodModeAllTimer = -1f;
                return;
            }

            if (!CanRunBugRoomTick()) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            float now = Time.unscaledTime;
            if (glitchRoomGodModeAllTimer > 0f && now < glitchRoomGodModeAllTimer) return;
            glitchRoomGodModeAllTimer = now + 0.20f;

            ProtectGlitchRoomEveryone(false);
        }

private static void ProtectGlitchRoomEveryone(bool notify)
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                {
                    if (notify) ShowNotification("<color=#FF0000>[GLITCH ROOM]</color> Host required.");
                    return;
                }

                if (!CanRunBugRoomTick() || PlayerControl.AllPlayerControls == null)
                {
                    if (notify) ShowNotification("<color=#FFAA00>[GLITCH ROOM]</color> Protection is unavailable now.");
                    return;
                }

                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null) return;

                int count = 0;
                foreach (PlayerControl target in PlayerControl.AllPlayerControls)
                {
                    if (target == null || target.Data == null || target.PlayerId >= 100) continue;
                    if (target.Data.Disconnected || target.Data.IsDead || target.protectedByGuardianId >= 0) continue;

                    try
                    {
                        local.RpcProtectPlayer(target, target.Data.DefaultOutfit.ColorId);
                        count++;
                    }
                    catch { }
                }

                if (!notify) return;
                if (count > 0)
                    ShowNotification($"<color=#00FFAA>[GLITCH ROOM]</color> Protected players: {count}.");
                else
                    ShowNotification("<color=#FFAA00>[GLITCH ROOM]</color> Everyone is already protected.");
            }
            catch
            {
                if (notify) ShowNotification("<color=#FF0000>[GLITCH ROOM]</color> Protect Everyone failed.");
            }
        }

private void TryGlitchRoomForcedProtectionTick()
        {
            if (glitchRoomProtectedPlayers.Count == 0)
            {
                glitchRoomProtectionTimer = -1f;
                return;
            }

            if (AmongUsClient.Instance == null ||
                !AmongUsClient.Instance.AmHost ||
                AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
            {
                glitchRoomProtectedPlayers.Clear();
                glitchRoomProtectionTimer = -1f;
                return;
            }

            if (!CanRunBugRoomTick()) return;

            float now = Time.unscaledTime;
            if (glitchRoomProtectionTimer > 0f && now < glitchRoomProtectionTimer) return;
            glitchRoomProtectionTimer = now + 0.10f;
            glitchRoomProtectionRemove.Clear();

            foreach (byte id in glitchRoomProtectedPlayers)
            {
                PlayerControl target = GameData.Instance?.GetPlayerById(id)?.Object;
                if (target == null || target.Data == null || target.Data.Disconnected || target.Data.IsDead)
                {
                    if (target != null && target.protectedByGuardianId >= 0)
                    {
                        try { target.RemoveProtection(); } catch { }
                    }

                    glitchRoomProtectionRemove.Add(id);
                    continue;
                }

                if (target.protectedByGuardianId >= 0) continue;

                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null) continue;
                try { local.RpcProtectPlayer(target, target.Data.DefaultOutfit.ColorId); } catch { }
            }

            foreach (byte id in glitchRoomProtectionRemove)
                glitchRoomProtectedPlayers.Remove(id);
        }

private static bool IsGlitchRoomProtected(PlayerControl target)
        {
            return target != null && glitchRoomProtectedPlayers.Contains(target.PlayerId);
        }

private static void ProtectGlitchRoomTarget(bool force)
        {
            try
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                {
                    ShowNotification("<color=#FF0000>[GLITCH ROOM]</color> Host required.");
                    return;
                }

                if (!CanRunBugRoomTick())
                {
                    ShowNotification("<color=#FFAA00>[GLITCH ROOM]</color> Protection is unavailable now.");
                    return;
                }

                PlayerControl local = PlayerControl.LocalPlayer;
                PlayerControl target = FindHostAutoKillTarget(local);
                bool validSelf = force &&
                                 target == local &&
                                 local != null &&
                                 local.Data != null &&
                                 !local.Data.Disconnected &&
                                 !local.Data.IsDead;
                if (local == null || target == null || (!validSelf && !IsBugRoomAngelTarget(target, local)))
                {
                    ShowNotification("<color=#FFAA00>[GLITCH ROOM]</color> Select a living target.");
                    return;
                }

                if (force)
                {
                    if (glitchRoomProtectedPlayers.Remove(target.PlayerId))
                    {
                        if (target.protectedByGuardianId >= 0) target.RemoveProtection();
                        ShowNotification($"<color=#FFAA00>[GLITCH ROOM]</color> Forced protection OFF: {target.Data.PlayerName}.");
                        return;
                    }

                    glitchRoomProtectedPlayers.Add(target.PlayerId);
                    if (target.protectedByGuardianId < 0)
                        local.RpcProtectPlayer(target, target.Data.DefaultOutfit.ColorId);
                    ShowNotification($"<color=#00FFAA>[GLITCH ROOM]</color> Forced protection ON: {target.Data.PlayerName}.");
                    return;
                }

                if (target.protectedByGuardianId >= 0)
                {
                    ShowNotification("<color=#FFAA00>[GLITCH ROOM]</color> Target is already protected.");
                    return;
                }

                GuardianAngelRole role = local.Data.Role as GuardianAngelRole;
                if (role == null || local.Data.Role.Role != RoleTypes.GuardianAngel)
                {
                    ShowNotification("<color=#FFAA00>[GLITCH ROOM]</color> Guardian Angel role required.");
                    return;
                }

                if (role.cooldownSecondsRemaining > 0f)
                {
                    ShowNotification($"<color=#FFAA00>[GLITCH ROOM]</color> Protect cooldown: {role.cooldownSecondsRemaining:0.0}s.");
                    return;
                }

                local.CmdCheckProtect(target);
                ShowNotification($"<color=#00FFAA>[GLITCH ROOM]</color> Guardian protection: {target.Data.PlayerName}.");
            }
            catch
            {
                ShowNotification("<color=#FF0000>[GLITCH ROOM]</color> Protection failed.");
            }
        }

private void ResetGlitchRoomState()
        {
            bugRoomAutoAngel = false;
            bugRoomAutoKillShield = false;
            bugRoomImpMeeting = false;
            glitchRoomBypassShield = false;
            glitchRoomGodMode = false;
            glitchRoomGodModeAll = false;
            bugRoomAngelTimer = -1f;
            bugRoomShieldKillTimer = -1f;
            bugRoomImpMeetingTimer = 0f;
            bugRoomImpMeetingDone = false;
            glitchRoomGodModeTimer = -1f;
            glitchRoomGodModeAllTimer = -1f;
            glitchRoomProtectionTimer = -1f;
            bugRoomAngelLastTargetId = byte.MaxValue;
            glitchRoomProtectedPlayers.Clear();
            glitchRoomProtectionRemove.Clear();

            try
            {
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == null || pc.protectedByGuardianId < 0) continue;
                        pc.RemoveProtection();
                    }
                }
            }
            catch { }

            PlayerControl_TurnOnProtection_Patch.ClearProtectionState();
            settingsDirty = true;
            ShowNotification("<color=#FFAA00>[GLITCH ROOM]</color> Angel state reset.");
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
            ShowNotification($"<color=#FF00FF>[GLITCH ROOM]</color> Auto Run {AutoHostAutoRunDelaySeconds:0.00}s enabled.");
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
                    if (pc == null || pc.Data == null) continue;
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
