using Unity.Netcode;
using UnityEngine;

namespace NetworkTankArena.Networking
{
    public enum MatchPhase : byte
    {
        WaitingForPlayers = 0,
        WaitingForReady = 1,
        Playing = 2
    }

    [DisallowMultipleComponent] // Prevents multiple instances of this component on the same GameObject
    [RequireComponent(typeof(NetworkObject))] // Ensures that a NetworkObject component is attached to the same GameObject
    public sealed class NetworkMatchState : NetworkBehaviour
    {
        private readonly NetworkVariable<MatchPhase> phase = new NetworkVariable<MatchPhase>(
            MatchPhase.WaitingForPlayers,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

        public MatchPhase Phase => phase.Value; // Public getter for the current match phase

        public bool CanChangeReady => Phase != MatchPhase.Playing; // Public getter to check if players can change their ready state


        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                EvaluateReadyGate();
            }
        }

        // Re-evaluate the pre-match gate from authoritative PlayerObjects.
        // The server is the only writer of MatchPhase.
        internal void EvaluateReadyGate()
        {
            if (!IsSpawned || !IsServer || Phase == MatchPhase.Playing)
            {
                return;
            }

            int connectedPlayers = 0;
            bool allReady = true;

            foreach (var pair in NetworkManager.ConnectedClients)
            {
                NetworkObject playerObject = pair.Value.PlayerObject; // pair = KeyValuePair<ulong, NetworkClient>
                if (playerObject == null || !playerObject.IsSpawned || !playerObject.TryGetComponent(out NetworkTankController player))
                {
                    continue;
                }

                connectedPlayers++;
                allReady &= player.IsReady;
            }

            MatchPhase next = connectedPlayers < 2
                ? MatchPhase.WaitingForPlayers
                : allReady
                    ? MatchPhase.Playing
                    : MatchPhase.WaitingForReady;

            if (phase.Value != next)
            {
                phase.Value = next;
            }
        }

        internal void NotifyPlayerDisconnected()
        {
            if (!IsSpawned || !IsServer || Phase == MatchPhase.Playing)
                return;
            
            phase.Value = MatchPhase.WaitingForPlayers;

        }


        private void OnGUI()
        {
            if (!IsSpawned || !IsClient)
                return;

            GUI.Box(new Rect(12f, 225f, 300f, 42f), $"Match Phase: {Phase}");
        }
    }
}