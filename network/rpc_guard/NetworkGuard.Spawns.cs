#nullable disable
using ElysiumModMenu;
using Hazel;
using InnerNet;
using System.Collections.Generic;
using UnityEngine;

namespace ElysiumNetGuard
{
internal static partial class NetworkGuard
{
	private const uint PlayerSpawnId = 4;

	private const int MaxPlayerSpawnBurst = 24;

	private const float PlayerSpawnBurstWindow = 4f;

	private const int MaxUnownedPlayerSpawnBurst = 6;

	private const float UnownedPlayerSpawnBurstWindow = 2f;

	private static readonly Queue<float> PlayerSpawnBurst = new Queue<float>();

	private static readonly Queue<float> UnownedPlayerSpawnBurst = new Queue<float>();

	private static int playerSpawnGameId;

	private static bool clientSpawnDropQueued;

	private static float clientSpawnDropAt;

	private static int clientSpawnDropGameId;

	internal static bool CheckUnownedSpawnFreeze(InnerNetClient client, MessageReader reader)
	{
		if (IsActiveInboundSenderProtected()) return true;

		if (!ElysiumModMenuGUI.enableUnownedSpawnGuard || client == null || reader == null ||
			(reader.Tag != 5 && reader.Tag != 6))
		{
			return true;
		}

		MessageReader copy = null;
		try
		{
			copy = MessageReader.Get(reader);
			int gameId = copy.ReadInt32();
			if (gameId == 0 || gameId != client.GameId)
			{
				return true;
			}

			if (reader.Tag == 6)
			{
				int target = copy.ReadPackedInt32();
				if (target != client.ClientId)
				{
					return true;
				}
			}

			if (playerSpawnGameId != gameId)
			{
				ResetPlayerSpawnGuard(gameId);
			}

			float now = Time.realtimeSinceStartup;
			PruneSpawnBurst(PlayerSpawnBurst, now, PlayerSpawnBurstWindow);
			PruneSpawnBurst(UnownedPlayerSpawnBurst, now, UnownedPlayerSpawnBurstWindow);

			int parts = 0;
			int ownerId = -2;
			bool blocked = false;
			bool unowned = false;
			while (copy.Position < copy.Length && parts++ < MaxTrackedGameDataParts)
			{
				MessageReader part = copy.ReadMessage();
				if (part == null) break;

				try
				{
					if (part.Tag != 4) continue;

					MessageReader spawn = null;
					try
					{
						spawn = MessageReader.Get(part);
						uint spawnId = spawn.ReadPackedUInt32();
						ownerId = spawn.ReadPackedInt32();
						if (spawnId != PlayerSpawnId) continue;

						PlayerSpawnBurst.Enqueue(now);
						unowned = ownerId != -2 &&
							ownerId != client.ClientId &&
							ownerId != client.HostId &&
							GetClientById(ownerId) == null;
						if (unowned)
						{
							UnownedPlayerSpawnBurst.Enqueue(now);
						}

						if (PlayerSpawnBurst.Count > MaxPlayerSpawnBurst ||
							UnownedPlayerSpawnBurst.Count > MaxUnownedPlayerSpawnBurst)
						{
							blocked = true;
							break;
						}
					}
					finally { spawn?.Recycle(); }
				}
				finally { part.Recycle(); }
			}

			if (!blocked)
			{
				return true;
			}

			string detail = unowned
				? $"{UnownedPlayerSpawnBurst.Count} unowned Player spawns in {UnownedPlayerSpawnBurstWindow:0}s, owner {ownerId}."
				: $"{PlayerSpawnBurst.Count} Player spawns in {PlayerSpawnBurstWindow:0}s.";

			PlayerSpawnBurst.Clear();
			UnownedPlayerSpawnBurst.Clear();

			if (client.AmHost)
			{
				int senderId = ResolveStrictActiveSenderClientId(GetActiveInboundSenderClientId());
				if (senderId >= 0)
				{
					RegisterInboundFloodDrop(senderId);
					BlockMessage(senderId, "Unowned spawn freeze", detail, "Ban");
				}
				else
				{
					BlockMessage(-1, "Unowned spawn freeze", detail, "Warn");
				}
			}
			else
			{
				QueueClientSpawnDrop(gameId, detail);
			}

			return false;
		}
		catch
		{
			return true;
		}
		finally
		{
			copy?.Recycle();
		}
	}

	internal static void UpdateClientSpawnDrop()
	{
		if (!ElysiumModMenuGUI.enableUnownedSpawnGuard)
		{
			clientSpawnDropQueued = false;
			return;
		}

		if (!clientSpawnDropQueued || Time.realtimeSinceStartup < clientSpawnDropAt)
		{
			return;
		}

		AmongUsClient client = AmongUsClient.Instance;
		if (client == null || client.AmHost || ((InnerNetClient)client).GameId != clientSpawnDropGameId)
		{
			clientSpawnDropQueued = false;
			return;
		}

		clientSpawnDropQueued = false;
		try
		{
			client.ExitGame(DisconnectReasons.ExitGame);
		}
		catch
		{
			try { ((InnerNetClient)client).EnqueueDisconnect(DisconnectReasons.ExitGame); }
			catch { }
		}
	}

	private static void QueueClientSpawnDrop(int gameId, string detail)
	{
		if (clientSpawnDropQueued) return;

		clientSpawnDropQueued = true;
		clientSpawnDropAt = Time.realtimeSinceStartup + 0.05f;
		clientSpawnDropGameId = gameId;
		GuardPlugin.Logger?.LogWarning((object)$"Unowned spawn freeze detected from host: {detail} Leaving lobby.");
	}

	private static void ResetPlayerSpawnGuard(int gameId)
	{
		playerSpawnGameId = gameId;
		PlayerSpawnBurst.Clear();
		UnownedPlayerSpawnBurst.Clear();
		clientSpawnDropQueued = false;
		clientSpawnDropGameId = 0;
	}

	private static void PruneSpawnBurst(Queue<float> burst, float now, float window)
	{
		while (burst.Count > 0 && now - burst.Peek() > window)
		{
			burst.Dequeue();
		}
	}
}
}
