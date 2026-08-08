#nullable disable
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI
    {
        private sealed class PlayerAccountEntry
        {
            public ExternalAccountType type;
            public string name;
            public string id;
            public string lastLogin;
        }

        private readonly Dictionary<string, List<PlayerAccountEntry>> playerAccountsByPuid = new Dictionary<string, List<PlayerAccountEntry>>();
        private readonly Dictionary<string, string> playerAccountTextByPuid = new Dictionary<string, string>();
        private readonly HashSet<string> playerAccountLoaded = new HashSet<string>();
        private readonly HashSet<string> playerAccountFailed = new HashSet<string>();
        private readonly HashSet<string> playerAccountQueued = new HashSet<string>();
        private readonly Queue<string> playerAccountQueue = new Queue<string>();
        private string playerAccountLookupPuid;
        private int playerAccountPendingId;
        private float playerAccountRequestAt;

        [HideFromIl2Cpp]
        private string GetPlayerHistoryAccountText(string puid)
        {
            UpdatePlayerAccountLookup();

            if (string.IsNullOrWhiteSpace(puid) || puid == "Unknown")
                return L("Accounts: unavailable", "Аккаунты: недоступны");

            puid = puid.Trim();
            if (playerAccountLoaded.Contains(puid))
            {
                if (playerAccountTextByPuid.TryGetValue(puid, out string text))
                    return $"{L("Accounts:", "Аккаунты:")} {text}";

                return playerAccountFailed.Contains(puid)
                    ? L("Accounts: cache unavailable", "Аккаунты: кэш недоступен")
                    : L("Accounts: not cached", "Аккаунты: нет в кэше");
            }

            LoadCachedPlayerAccounts(puid);
            UpdatePlayerAccountLookup();
            if (!playerAccountLoaded.Contains(puid))
                return L("Accounts: searching...", "Аккаунты: поиск...");

            if (playerAccountTextByPuid.TryGetValue(puid, out string accountText))
                return $"{L("Accounts:", "Аккаунты:")} {accountText}";

            return playerAccountFailed.Contains(puid)
                ? L("Accounts: cache unavailable", "Аккаунты: кэш недоступен")
                : L("Accounts: not cached", "Аккаунты: нет в кэше");
        }

        [HideFromIl2Cpp]
        private void LoadCachedPlayerAccounts(string puid)
        {
            if (playerAccountLoaded.Contains(puid) || playerAccountQueued.Contains(puid) || playerAccountLookupPuid == puid) return;

            playerAccountsByPuid[puid] = new List<PlayerAccountEntry>();
            ReadLocalPlayerAccounts(puid);
            ReadCachedPlayerAccounts(puid);
            playerAccountQueued.Add(puid);
            playerAccountQueue.Enqueue(puid);
        }

        [HideFromIl2Cpp]
        private void ReadLocalPlayerAccounts(string puid)
        {
            try
            {
                EOSManager eos = EOSManager.Instance;
                ProductUserId local = eos?.userId;
                if (local == null || local.ToString() != puid) return;

                if (eos.linkedExternalAccounts != null)
                {
                    for (int i = 0; i < eos.linkedExternalAccounts.Count; i++)
                    {
                        try
                        {
                            AddPlayerAccount(puid, eos.linkedExternalAccounts[i]);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Instance?.Log?.LogWarning((object)$"[ACCOUNTS] Local account {i} read failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                playerAccountFailed.Add(puid);
                Plugin.Instance?.Log?.LogWarning((object)$"[ACCOUNTS] Local cache read failed: {ex.Message}");
            }
        }

        [HideFromIl2Cpp]
        private void ReadCachedPlayerAccounts(string puid)
        {
            try
            {
                EOSManager eos = EOSManager.Instance;
                var connect = eos?.PlatformInterface?.GetConnectInterface();
                ProductUserId targetPuid = ProductUserId.FromString(puid);
                if (connect == null || targetPuid == null || !targetPuid.IsValid())
                {
                    playerAccountFailed.Add(puid);
                    return;
                }

                var countOptions = new GetProductUserExternalAccountCountOptions
                {
                    TargetUserId = targetPuid
                };
                uint count = connect.GetProductUserExternalAccountCount(ref countOptions);
                for (uint i = 0; i < count; i++)
                {
                    try
                    {
                        var copyOptions = new CopyProductUserExternalAccountByIndexOptions
                        {
                            TargetUserId = targetPuid,
                            ExternalAccountInfoIndex = i
                        };
                        Result result = connect.CopyProductUserExternalAccountByIndex(ref copyOptions, out var info);
                        if (result == Result.Success && info.HasValue)
                            AddPlayerAccount(puid, info.Value);
                        else if (result != Result.Success)
                            Plugin.Instance?.Log?.LogWarning((object)$"[ACCOUNTS] EOS account {i}/{count} copy failed: {result}");
                    }
                    catch (Exception ex)
                    {
                        playerAccountFailed.Add(puid);
                        Plugin.Instance?.Log?.LogWarning((object)$"[ACCOUNTS] EOS account {i}/{count} read failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                playerAccountFailed.Add(puid);
                Plugin.Instance?.Log?.LogWarning((object)$"[ACCOUNTS] EOS cache read failed: {ex.Message}");
            }
        }

        [HideFromIl2Cpp]
        private void UpdatePlayerAccountLookup()
        {
            if (playerAccountPendingId != 0)
            {
                if (!PlayerAccountNative.TryTake(playerAccountPendingId, out string puid, out int result))
                {
                    if (UnityEngine.Time.realtimeSinceStartup - playerAccountRequestAt < 12f)
                        return;

                    PlayerAccountNative.Forget(playerAccountPendingId);
                    puid = playerAccountLookupPuid;
                    result = -1;
                }

                playerAccountPendingId = 0;
                if (result == (int)Result.Success)
                    ReadCachedPlayerAccounts(puid);
                else
                {
                    playerAccountFailed.Add(puid);
                    Plugin.Instance?.Log?.LogWarning((object)$"[ACCOUNTS] PUID mapping failed for {puid}: {result}");
                }

                FinishPlayerAccountLookup(puid);
                playerAccountQueued.Remove(puid);
                playerAccountLookupPuid = null;
            }

            if (string.IsNullOrEmpty(playerAccountLookupPuid))
            {
                if (playerAccountQueue.Count == 0) return;
                playerAccountLookupPuid = playerAccountQueue.Dequeue();
            }

            try
            {
                EOSManager eos = EOSManager.Instance;
                var connect = eos?.PlatformInterface?.GetConnectInterface();
                ProductUserId local = eos?.userId;
                ProductUserId target = ProductUserId.FromString(playerAccountLookupPuid);
                if (connect == null || local == null || target == null || !local.IsValid() || !target.IsValid())
                {
                    playerAccountFailed.Add(playerAccountLookupPuid);
                    FinishPlayerAccountLookup(playerAccountLookupPuid);
                    playerAccountQueued.Remove(playerAccountLookupPuid);
                    playerAccountLookupPuid = null;
                    return;
                }

                playerAccountPendingId = PlayerAccountNative.Start(
                    connect.InnerHandle,
                    local.InnerHandle,
                    target.InnerHandle,
                    playerAccountLookupPuid);
                playerAccountRequestAt = UnityEngine.Time.realtimeSinceStartup;
            }
            catch (Exception ex)
            {
                playerAccountFailed.Add(playerAccountLookupPuid);
                Plugin.Instance?.Log?.LogWarning((object)$"[ACCOUNTS] Native PUID lookup failed: {ex.Message}");
                FinishPlayerAccountLookup(playerAccountLookupPuid);
                playerAccountQueued.Remove(playerAccountLookupPuid);
                playerAccountLookupPuid = null;
                playerAccountPendingId = 0;
            }
        }

        [HideFromIl2Cpp]
        private void AddPlayerAccount(string puid, ExternalAccountInfo account)
        {
            if (account == null) return;

            ExternalAccountType type = account.AccountIdType;
            string name = string.Empty;
            string id = string.Empty;
            string lastLogin = string.Empty;

            try
            {
                if (account.DisplayName != null)
                    name = account.DisplayName.ToString();
            }
            catch (Exception ex)
            {
                Plugin.Instance?.Log?.LogWarning((object)$"[ACCOUNTS] {type} name read failed: {ex.Message}");
            }

            try
            {
                if (account.AccountId != null)
                    id = account.AccountId.ToString();
            }
            catch (Exception ex)
            {
                Plugin.Instance?.Log?.LogWarning((object)$"[ACCOUNTS] {type} id read failed: {ex.Message}");
            }

            try
            {
                var login = account.LastLoginTime;
                if (login.HasValue)
                    lastLogin = login.Value.ToString("dd.MM.yyyy HH:mm:ss");
            }
            catch (Exception ex)
            {
                Plugin.Instance?.Log?.LogWarning((object)$"[ACCOUNTS] {type} login time read failed: {ex.Message}");
            }

            AddPlayerAccount(puid, type, name, id, lastLogin);
        }

        [HideFromIl2Cpp]
        private void AddPlayerAccount(string puid, ExternalAccountType type, string name, string id, string lastLogin)
        {
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(id)) return;

            if (!playerAccountsByPuid.TryGetValue(puid, out List<PlayerAccountEntry> accounts))
            {
                accounts = new List<PlayerAccountEntry>();
                playerAccountsByPuid[puid] = accounts;
            }

            PlayerAccountEntry entry = accounts.Find(x =>
                x.type == type &&
                ((!string.IsNullOrWhiteSpace(id) && x.id == id) ||
                 (!string.IsNullOrWhiteSpace(name) && x.name == name &&
                  (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(x.id)))));
            if (entry == null)
            {
                entry = new PlayerAccountEntry();
                accounts.Add(entry);
            }

            entry.type = type;
            if (!string.IsNullOrWhiteSpace(name)) entry.name = name;
            if (!string.IsNullOrWhiteSpace(id)) entry.id = id;
            if (!string.IsNullOrWhiteSpace(lastLogin)) entry.lastLogin = lastLogin;
        }

        [HideFromIl2Cpp]
        private void FinishPlayerAccountLookup(string puid)
        {
            if (playerAccountsByPuid.TryGetValue(puid, out List<PlayerAccountEntry> accounts) && accounts.Count > 0)
            {
                string[] names = new string[accounts.Count];
                var typeCounts = new Dictionary<ExternalAccountType, int>();
                for (int i = 0; i < accounts.Count; i++)
                {
                    PlayerAccountEntry account = accounts[i];
                    string name = string.IsNullOrWhiteSpace(account.name) ? account.id : account.name;
                    typeCounts.TryGetValue(account.type, out int count);
                    count++;
                    typeCounts[account.type] = count;
                    string type = AccountTypeName(account.type);
                    if (count > 1) type += $" ({count})";
                    names[i] = string.IsNullOrWhiteSpace(account.lastLogin)
                        ? $"{type}: {name}"
                        : $"{type}: {name} [{account.lastLogin}]";
                }
                playerAccountTextByPuid[puid] = string.Join(" | ", names);
            }

            playerAccountLoaded.Add(puid);
        }

        private void ClearPlayerAccountCache()
        {
            if (playerAccountPendingId != 0)
                PlayerAccountNative.Forget(playerAccountPendingId);

            playerAccountsByPuid.Clear();
            playerAccountTextByPuid.Clear();
            playerAccountLoaded.Clear();
            playerAccountFailed.Clear();
            playerAccountQueued.Clear();
            playerAccountQueue.Clear();
            playerAccountLookupPuid = null;
            playerAccountPendingId = 0;
            playerAccountRequestAt = 0f;
        }

        private static string AccountTypeName(ExternalAccountType type)
        {
            switch (type)
            {
                case ExternalAccountType.Epic: return "Epic";
                case ExternalAccountType.Steam: return "Steam";
                case ExternalAccountType.Psn: return "PlayStation";
                case ExternalAccountType.Xbl: return "Microsoft / Xbox";
                case ExternalAccountType.Discord: return "Discord";
                case ExternalAccountType.Gog: return "GOG";
                case ExternalAccountType.Nintendo: return "Nintendo";
                case ExternalAccountType.Uplay: return "Ubisoft";
                case ExternalAccountType.Openid: return "OpenID";
                case ExternalAccountType.Apple: return "Apple";
                case ExternalAccountType.Google: return "Google";
                case ExternalAccountType.Oculus: return "Oculus";
                case ExternalAccountType.Itchio: return "Itch";
                case ExternalAccountType.Amazon: return "Amazon";
                case ExternalAccountType.Viveport: return "Viveport";
                default: return type.ToString();
            }
        }
    }

    internal static class PlayerAccountNative
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct QueryOptions
        {
            public int ApiVersion;
            public IntPtr LocalUserId;
            public int AccountIdTypeDeprecated;
            public IntPtr ProductUserIds;
            public uint ProductUserIdCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct QueryCallbackInfo
        {
            public int ResultCode;
            public IntPtr ClientData;
            public IntPtr LocalUserId;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void QueryCallback(IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void QueryProductUserIdMappings(
            IntPtr handle,
            ref QueryOptions options,
            IntPtr clientData,
            QueryCallback completion);

        private sealed class QueryState
        {
            public string puid;
            public IntPtr ids;
            public bool complete;
            public bool discard;
            public int result;
        }

        private static readonly object sync = new object();
        private static readonly Dictionary<int, QueryState> queries = new Dictionary<int, QueryState>();
        private static readonly QueryCallback callback = OnQueryComplete;
        private static QueryProductUserIdMappings queryMappings;
        private static IntPtr eosLibrary;
        private static int nextId;

        private static void LoadEos()
        {
            if (queryMappings != null) return;

            lock (sync)
            {
                if (queryMappings != null) return;

                bool is64 = IntPtr.Size == 8;
                string path = System.IO.Path.Combine(UnityEngine.Application.dataPath,
                    "Plugins", is64 ? "x86_64" : "x86",
                    is64 ? "EOSSDK-Win64-Shipping.dll" : "EOSSDK-Win32-Shipping.dll");
                string entry = is64
                    ? "EOS_Connect_QueryProductUserIdMappings"
                    : "_EOS_Connect_QueryProductUserIdMappings@16";
                eosLibrary = NativeLibrary.Load(path);
                IntPtr ptr = NativeLibrary.GetExport(eosLibrary, entry);
                queryMappings = Marshal.GetDelegateForFunctionPointer<QueryProductUserIdMappings>(ptr);
            }
        }

        internal static int Start(IntPtr connect, IntPtr local, IntPtr target, string puid)
        {
            LoadEos();

            IntPtr ids = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(ids, target);

            int id;
            lock (sync)
            {
                id = ++nextId;
                if (id == 0) id = ++nextId;
                queries[id] = new QueryState
                {
                    puid = puid,
                    ids = ids
                };
            }

            var options = new QueryOptions
            {
                ApiVersion = 2,
                LocalUserId = local,
                AccountIdTypeDeprecated = 0,
                ProductUserIds = ids,
                ProductUserIdCount = 1
            };

            try
            {
                queryMappings(connect, ref options, new IntPtr(id), callback);
                return id;
            }
            catch
            {
                lock (sync)
                    queries.Remove(id);
                Marshal.FreeHGlobal(ids);
                throw;
            }
        }

        internal static bool TryTake(int id, out string puid, out int result)
        {
            lock (sync)
            {
                if (queries.TryGetValue(id, out QueryState state) && state.complete)
                {
                    puid = state.puid;
                    result = state.result;
                    queries.Remove(id);
                    return true;
                }
            }

            puid = null;
            result = 0;
            return false;
        }

        internal static void Forget(int id)
        {
            lock (sync)
            {
                if (!queries.TryGetValue(id, out QueryState state)) return;
                if (state.complete)
                    queries.Remove(id);
                else
                    state.discard = true;
            }
        }

        private static void OnQueryComplete(IntPtr data)
        {
            try
            {
                if (data == IntPtr.Zero) return;
                QueryCallbackInfo info = Marshal.PtrToStructure<QueryCallbackInfo>(data);
                int id = info.ClientData.ToInt32();

                lock (sync)
                {
                    if (!queries.TryGetValue(id, out QueryState state)) return;
                    state.result = info.ResultCode;
                    state.complete = true;
                    if (state.ids != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(state.ids);
                        state.ids = IntPtr.Zero;
                    }
                    if (state.discard)
                        queries.Remove(id);
                }
            }
            catch { }
        }
    }

}
