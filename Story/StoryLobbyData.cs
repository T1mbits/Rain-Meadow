using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using static RainMeadow.OnlineResource;

namespace RainMeadow
{
    //playerSessionData - passage
    internal class StoryLobbyData : OnlineResource.ResourceData
    {
        public StoryLobbyData(OnlineResource resource) : base(resource) { }

        internal override ResourceDataState MakeState()
        {
            return new State(this, resource);
        }

        public class State : ResourceDataState
        {
            [OnlineField(nullable=true)]
            public string? defaultDenPos;
            [OnlineField]
            public bool isInGame;
            [OnlineField]
            public bool changedRegions;
            [OnlineField]
            public SlugcatStats.Name currentCampaign;
            [OnlineField]
            public int cycleNumber;
            [OnlineField]
            public bool didStartCycle;
            [OnlineField]
            public bool reinforcedKarma;
            [OnlineField]
            public int karmaCap;
            [OnlineField]
            public int karma;
            [OnlineField]
            public bool theGlow;
            [OnlineField]
            public int food;
            [OnlineField]
            public int quarterfood;
            [OnlineField]
            public int mushroomCounter;
            [OnlineField]
            public Dictionary<string, int> ghostsTalkedTo;
            [OnlineField]
            public Dictionary<ushort, ushort[]> consumedItems;
            [OnlineField]
            public Dictionary<string, bool> storyBoolRemixSettings;
            [OnlineField]
            public Dictionary<string, float> storyFloatRemixSettings;
            [OnlineField]
            public Dictionary<string, int> storyIntRemixSettings;


            public State() {}

            public State(StoryLobbyData storyLobbyData, OnlineResource onlineResource)
            {
                StoryGameMode storyGameMode = (onlineResource as Lobby).gameMode as StoryGameMode;
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;

                storyBoolRemixSettings = storyGameMode.storyBoolRemixSettings;
                storyFloatRemixSettings = storyGameMode.storyFloatRemixSettings;
                storyIntRemixSettings = storyGameMode.storyIntRemixSettings;

                defaultDenPos = storyGameMode.defaultDenPos;
                currentCampaign = storyGameMode.currentCampaign;
                consumedItems = storyGameMode.consumedItems;
                ghostsTalkedTo = storyGameMode.ghostsTalkedTo;
                isInGame = RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame;
                changedRegions = storyGameMode.changedRegions;
                didStartCycle = storyGameMode.didStartCycle;

                if (currentGameState?.session is StoryGameSession storySession)
                {
                    cycleNumber = storySession.saveState.cycleNumber;
                    karma = storySession.saveState.deathPersistentSaveData.karma;
                    karmaCap = storySession.saveState.deathPersistentSaveData.karmaCap;
                    theGlow = storySession.saveState.theGlow;
                    reinforcedKarma = storySession.saveState.deathPersistentSaveData.reinforcedKarma;
                }

                food = (currentGameState?.Players[0].state as PlayerState)?.foodInStomach ?? 0;
                quarterfood = (currentGameState?.Players[0].state as PlayerState)?.quarterFoodPoints ?? 0;
                mushroomCounter = (currentGameState?.Players[0].realizedCreature as Player)?.mushroomCounter ?? 0;
            }

            internal override Type GetDataType() => typeof(StoryLobbyData);

            internal override void ReadTo(ResourceData data)
            {
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
                var playerState = currentGameState?.Players[0].state as PlayerState;
                var lobby = data.resource as Lobby;
                var storyGameMode = lobby.gameMode as StoryGameMode;

                if (playerState != null)
                {
                    playerState.foodInStomach = food;
                    playerState.quarterFoodPoints = quarterfood;
                }
                if ((currentGameState?.Players[0].realizedCreature is Player player))
                {
                    player.mushroomCounter = mushroomCounter;
                }

                if (currentGameState?.session is StoryGameSession storySession)
                {
                    storySession.saveState.cycleNumber = cycleNumber;
                    storySession.saveState.deathPersistentSaveData.karma = karma;
                    storySession.saveState.deathPersistentSaveData.karmaCap = karmaCap;
                    storySession.saveState.deathPersistentSaveData.reinforcedKarma = reinforcedKarma;
                    storySession.saveState.theGlow = theGlow;
                    if ((RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame rainWorldGame))
                    {
                        if(rainWorldGame.Players[0].realizedCreature != null)
                            (rainWorldGame.Players[0].realizedCreature as Player).glowing = theGlow;
                    }
                }

                storyGameMode.storyBoolRemixSettings = storyBoolRemixSettings;
                storyGameMode.storyFloatRemixSettings = storyFloatRemixSettings;
                storyGameMode.storyIntRemixSettings = storyIntRemixSettings;

                storyGameMode.defaultDenPos = defaultDenPos;
                storyGameMode.currentCampaign = currentCampaign;
                storyGameMode.consumedItems = consumedItems;
                storyGameMode.ghostsTalkedTo = ghostsTalkedTo;
                storyGameMode.isInGame = isInGame;
                storyGameMode.changedRegions = changedRegions;
                storyGameMode.didStartCycle = didStartCycle;
            }
        }
    }
}
