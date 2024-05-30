using RWCustom;
using System;
using System.Linq;
using UnityEngine;
namespace RainMeadow
{
    public partial class RainMeadow
    {
        public void GameplayHooks()
        {
            On.ShelterDoor.Close += ShelterDoorOnClose;
            On.Creature.Update += CreatureOnUpdate;
            On.Creature.Violence += CreatureOnViolence;
            On.Creature.Grasp.ctor += GraspOnctor;
            On.PhysicalObject.Grabbed += PhysicalObjectOnGrabbed;


            On.PhysicalObject.HitByWeapon += PhysicalObject_HitByWeapon;
            On.PhysicalObject.HitByExplosion += PhysicalObject_HitByExplosion;
        }
        private void PhysicalObject_HitByExplosion(On.PhysicalObject.orig_HitByExplosion orig, PhysicalObject self, float hitFac, Explosion explosion, int hitChunk)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, hitFac, explosion, hitChunk);
                return;
            }

            // explosion.room is the most stable source of truth for room data. Other options sometimes null ref
            if (!RoomSession.map.TryGetValue(explosion.room.abstractRoom, out var room))
            {
                Error("Error getting room for explosion!");

            }
            if (!room.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var objectHit))
                {
                    Error("Error getting target of explosion object hit");

                }
                if (!OnlinePhysicalObject.map.TryGetValue(explosion.sourceObject.abstractPhysicalObject, out var sourceObject))
                {
                    Error("Error getting source object for explosion");
                }

                if (explosion.killTagHolder == null)
                {
                    orig(self, hitFac, explosion, hitChunk); // Safely kills target when explosive spear is stuck in them stuck in them.
                    return;
                }


                if (!OnlinePhysicalObject.map.TryGetValue(explosion.killTagHolder.abstractPhysicalObject, out var onlineCreature)) // to pass OnlinePhysicalObject data to convert to OnlineCreature over the wire
                {

                    Error("Error getting kill tag holder");
                }

                if (objectHit != null)
                {
                    if (!room.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(OnlinePhysicalObject.HitByExplosion, objectHit, sourceObject, explosion.pos, explosion.lifeTime, explosion.rad, explosion.force, explosion.damage, explosion.stun, explosion.deafen, onlineCreature, explosion.killTagHolderDmgFactor, explosion.minStun, explosion.backgroundNoise, hitFac, hitChunk)))
                    {
                        room.owner.InvokeRPC(OnlinePhysicalObject.HitByExplosion, objectHit, sourceObject, explosion.pos, explosion.lifeTime, explosion.rad, explosion.force, explosion.damage, explosion.stun, explosion.deafen, onlineCreature, explosion.killTagHolderDmgFactor, explosion.minStun, explosion.backgroundNoise, hitFac, hitChunk);
                    }
                }
            }

            orig(self, hitFac, explosion, hitChunk);
        }

        // TODO: What ever the solution is, it needs to encapuslate damage dealt to Slugcats so I don't have to hook each one. Maybe it's under Physical Object, but need to figure out why damage is not being assigned to the player
        // The RPC is delivering data
        // Players can hurt other animals
        // Meadow isn't bleeding logic
        // Players deal 1 damage
        // It feels like we're doing a return somewhere for no baffling reason. Only on other players
        private void PhysicalObject_HitByWeapon(On.PhysicalObject.orig_HitByWeapon orig, PhysicalObject self, Weapon weapon)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, weapon);
                return;
            }

            RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
            if (!room.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var objectHit);
                OnlinePhysicalObject.map.TryGetValue(weapon.abstractPhysicalObject, out var abstWeapon);
                room.owner.InvokeRPC(OnlinePhysicalObject.HitByWeapon, objectHit, abstWeapon);
            }

            orig(self, weapon);


        }

        private void ShelterDoorOnClose(On.ShelterDoor.orig_Close orig, ShelterDoor self)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self);
                return;
            }

            if (OnlineManager.lobby.gameMode is StoryGameMode storyGameMode)
            {
                //for now force all players to be in the shelter to close the door.
                var playerIDs = OnlineManager.lobby.participants.Keys.Select(p => p.inLobbyId).ToList();
                var readyWinPlayers = storyGameMode.readyForWinPlayers.ToList();

                foreach (var playerID in playerIDs)
                {
                    if (!readyWinPlayers.Contains(playerID)) return;
                }
                var storyClientSettings = storyGameMode.clientSettings as StoryClientSettings;
                storyClientSettings.myLastDenPos = self.room.abstractRoom.name;
                if (OnlineManager.lobby.isOwner)
                {
                    (OnlineManager.lobby.gameMode as StoryGameMode).defaultDenPos = self.room.abstractRoom.name;
                }
                storyGameMode.changedRegions = false;
            }
            else
            {
                var scug = self.room.game.Players.First(); //needs to be changed if we want to support Jolly
                var realizedScug = (Player)scug.realizedCreature;
                if (realizedScug == null || !self.room.PlayersInRoom.Contains(realizedScug)) return;
                if (!realizedScug.readyForWin) return;
            }
            orig(self);
        }

        private void CreatureOnUpdate(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            orig(self, eu);

            if (OnlineManager.lobby == null) return;
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineCreature))
            {
                Error($"Creature {self} {self.abstractPhysicalObject.ID} doesn't exist in online space!");
                return;
            }
            if (OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                if (EmoteDisplayer.map.TryGetValue(self, out var displayer))
                {
                    displayer.OnUpdate(); // so this only updates while the creature is in-room, what about creatures in pipes though
                }

                if (self is AirBreatherCreature breather) breather.lungs = 1f;

                if (self.room != null)
                {
                    // fall out of world handling
                    float num = -self.bodyChunks[0].restrictInRoomRange + 1f;
                    if (self is Player && self.bodyChunks[0].restrictInRoomRange == self.bodyChunks[0].defaultRestrictInRoomRange)
                    {
                        if ((self as Player).bodyMode == Player.BodyModeIndex.WallClimb)
                        {
                            num = Mathf.Max(num, -250f);
                        }
                        else
                        {
                            num = Mathf.Max(num, -500f);
                        }
                    }
                    if (self.bodyChunks[0].pos.y < num && (!self.room.water || self.room.waterInverted || self.room.defaultWaterLevel < -10) && (!self.Template.canFly || self.Stunned || self.dead) && (self is Player || !self.room.game.IsArenaSession || self.room.game.GetArenaGameSession.chMeta == null || !self.room.game.GetArenaGameSession.chMeta.oobProtect))
                    {
                        RainMeadow.Debug("fall out of world prevention: " + self);
                        var room = self.room;
                        self.RemoveFromRoom();
                        room.CleanOutObjectNotInThisRoom(self); // we need it this frame
                        var node = self.coord.abstractNode;
                        if (node > room.abstractRoom.exits) node = UnityEngine.Random.Range(0, room.abstractRoom.exits);
                        self.SpitOutOfShortCut(room.ShortcutLeadingToNode(node).startCoord.Tile, room, true);
                    }
                }
            }

            if (OnlineManager.lobby.gameMode is ArenaCompetitiveGameMode) // Need to test this with creatures on
            {
                if (self.room != null)
                {
                    // fall out of world handling
                    float num = -self.bodyChunks[0].restrictInRoomRange + 1f;
                    if (self is Player && self.bodyChunks[0].restrictInRoomRange == self.bodyChunks[0].defaultRestrictInRoomRange)
                    {
                        if ((self as Player).bodyMode == Player.BodyModeIndex.WallClimb)
                        {
                            num = Mathf.Max(num, -250f);
                        }
                        else
                        {
                            num = Mathf.Max(num, -500f);
                        }
                    }
                    if (self.bodyChunks[0].pos.y < num && (!self.room.water || self.room.waterInverted || self.room.defaultWaterLevel < -10) && (!self.Template.canFly || self.Stunned || self.dead) && (self is Player || self.room.game.GetArenaGameSession.chMeta == null || !self.room.game.GetArenaGameSession.chMeta.oobProtect))
                    {
                        RainMeadow.Debug("prevent abstract creature destroy: " + self); // need this so that we don't release the world session on death
                        self.Die();
                        self.abstractPhysicalObject.LoseAllStuckObjects();
                        self.State.alive = false;
                    }
                }
            }

            if (onlineCreature.isMine && self.grasps != null)
            {
                foreach (var grasp in self.grasps)
                {
                    if (grasp == null) continue;
                    if (!OnlinePhysicalObject.map.TryGetValue(grasp.grabbed.abstractPhysicalObject, out var onlineGrabbed))
                    {
                        Error($"Grabbed object {grasp.grabbed.abstractPhysicalObject} {grasp.grabbed.abstractPhysicalObject.ID} doesn't exist in online space!");
                        continue;
                    }
                    if (!onlineGrabbed.isMine && onlineGrabbed.isTransferable && !onlineGrabbed.isPending)
                    {
                        if (grasp.grabbed is not Creature) // Non-Creetchers cannot be grabbed by multiple creatures
                        {
                            grasp.Release();
                            return;
                        }

                        var grabbersOtherThanMe = grasp.grabbed.grabbedBy.Select(x => x.grabber).Where(x => x != self);
                        foreach (var grabbers in grabbersOtherThanMe)
                        {
                            if (!OnlinePhysicalObject.map.TryGetValue(grabbers.abstractPhysicalObject, out var tempEntity))
                            {
                                Error($"Other grabber {grabbers.abstractPhysicalObject} {grabbers.abstractPhysicalObject.ID} doesn't exist in online space!");
                                continue;
                            }
                            if (!tempEntity.isMine) return;
                        }
                        // If no remotes holding the entity, request it
                        onlineGrabbed.Request();
                    }
                }
            }
        }

        private void CreatureOnViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionandmomentum, BodyChunk hitchunk, PhysicalObject.Appendage.Pos hitappendage, Creature.DamageType type, float damage, float stunbonus)
        {

            // TODO:
            // MakeState would manage the entity states. It's possible there's an issue with this updating? Manually making a creature dead is not sync'd.
            if (OnlineManager.lobby == null)
            {
                orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
                return;
            }
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineVictim) || onlineVictim is not OnlineCreature)
            {
                Error($"Chunk owner {self} - {self.abstractPhysicalObject.ID} doesn't exist in online space!");
                orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
                return;
            }
            var room = self.room;
            if (room != null && room.updateIndex <= room.updateList.Count)
            {
                PhysicalObject trueVillain = null;
                var suspect = room.updateList[room.updateIndex];
                if (suspect is Explosion explosion) trueVillain = explosion.sourceObject;
                else if (suspect is PhysicalObject villainObject) trueVillain = villainObject;
                if (trueVillain != null)
                {
                    if (!OnlinePhysicalObject.map.TryGetValue(trueVillain.abstractPhysicalObject, out var onlineTrueVillain))
                    {
                        Error($"True villain {trueVillain} - {trueVillain.abstractPhysicalObject.ID} doesn't exist in online space!");
                        orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
                        return;
                    }
                    if ((onlineTrueVillain.owner.isMe || onlineTrueVillain.isPending) && !onlineVictim.owner.isMe) // I'm violencing a remote entity
                    {
                        if (source != null && !OnlinePhysicalObject.map.TryGetValue(source.owner.abstractPhysicalObject, out var weapon))
                        {
                            Error($"Source {source.owner} - {source.owner.abstractPhysicalObject.ID} doesn't exist in online space!");
                            orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
                            return;
                        }
                        // Notify entity owner of violence

                        Debug("DAMAGE " + damage + "FROM " + onlineTrueVillain +  "TO " + onlineVictim + "WITH " + type);


                        if (!onlineTrueVillain.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(OnlineCreature.CreatureViolence, onlineVictim, onlineTrueVillain, hitchunk?.index, hitappendage == null ? null : new AppendageRef(hitappendage), directionandmomentum, type, damage, stunbonus)))
                        {
                            onlineTrueVillain.owner.InvokeRPC(OnlineCreature.CreatureViolence, onlineVictim, onlineTrueVillain, hitchunk?.index, hitappendage == null ? null : new AppendageRef(hitappendage), directionandmomentum, type, damage, stunbonus);
                        }
                        /*
                                                    onlineTrueVillain.owner.InvokeRPC(OnlineCreature.CreatureViolence, onlineVictim, onlineTrueVillain, hitchunk?.index, hitappendage == null ? null : new AppendageRef(hitappendage), directionandmomentum, type, damage, stunbonus);
                                                    return;
                                                }*/

                    }
                    if (!onlineTrueVillain.owner.isMe) return; // Remote entity will send an event
                }
            }
            orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
        }

        private void GraspOnctor(On.Creature.Grasp.orig_ctor orig, Creature.Grasp self, Creature grabber, PhysicalObject grabbed, int graspused, int chunkgrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            orig(self, grabber, grabbed, graspused, chunkgrabbed, shareability, dominance, pacifying);
            if (OnlineManager.lobby == null) return;
            if (!OnlinePhysicalObject.map.TryGetValue(grabber.abstractPhysicalObject, out var onlineGrabber)) throw new InvalidOperationException("Grabber doesn't exist in online space!");
            if (!OnlinePhysicalObject.map.TryGetValue(grabbed.abstractPhysicalObject, out var onlineGrabbed)) throw new InvalidOperationException("Grabbed thing doesn't exist in online space!");

            if (onlineGrabber.isMine && !onlineGrabbed.isMine && onlineGrabbed.isTransferable && !onlineGrabbed.isPending)
            {
                onlineGrabbed.Request();
            }
        }

        private void PhysicalObjectOnGrabbed(On.PhysicalObject.orig_Grabbed orig, PhysicalObject self, Creature.Grasp grasp)
        {
            orig(self, grasp);
            if (OnlineManager.lobby == null) return;
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidOperationException("Entity doesn't exist in online space!");
            if (!OnlinePhysicalObject.map.TryGetValue(grasp.grabber.abstractPhysicalObject, out var onlineGrabber)) throw new InvalidOperationException("Grabber doesn't exist in online space!");

            if (!onlineEntity.isTransferable && onlineEntity.isMine)
            {
                if (!onlineGrabber.isMine && onlineGrabber.isTransferable && !onlineGrabber.isPending)
                {
                    onlineGrabber.Request(); // If I've been grabbed and I'm not transferrable, but my grabber is, request him
                }
            }
        }
    }
}
